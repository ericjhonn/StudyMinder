using Microsoft.EntityFrameworkCore;
using StudyMinder.Data;
using StudyMinder.Models;
using StudyMinder.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudyMinder.Services
{
    public class CicloEstudoService
    {
        private readonly StudyMinderContext _context;

        public CicloEstudoService(StudyMinderContext context)
        {
            _context = context;
        }
        public async Task<List<CicloEstudo>> ObterCicloAsync()
        {
            // 1. Carregar o ciclo básico
            var ciclo = await _context.Set<CicloEstudo>()
                .Include(c => c.Assunto)
                .ThenInclude(a => a.Disciplina)
                .OrderBy(c => c.Ordem)
                .ToListAsync();

            if (!ciclo.Any()) return ciclo;

            // 2. Obter IDs dos assuntos carregados
            var assuntoIds = ciclo.Select(c => c.AssuntoId).ToList();

            // 3. Buscar o último estudo de cada assunto (Query otimizada)
            // Agrupa por assunto e pega o mais recente (DataTicks descendente)
            var ultimosEstudos = await _context.Estudos
                .Where(e => assuntoIds.Contains(e.AssuntoId))
                .GroupBy(e => e.AssuntoId)
                .Select(g => g.OrderByDescending(e => e.DataTicks).First())
                .ToListAsync();

            // 4. Associar em memória
            foreach (var item in ciclo)
            {
                item.UltimoEstudo = ultimosEstudos.FirstOrDefault(e => e.AssuntoId == item.AssuntoId);
            }

            return ciclo;
        }

        public async Task<CicloEstudo?> ObterProximoSugestaoAsync()
        {
            var ciclo = await ObterCicloAsync();
            if (!ciclo.Any()) return null;

            var idsCiclo = ciclo.Select(c => c.AssuntoId).ToList();

            // Buscar o último estudo realizado que pertença a qualquer assunto do ciclo
            var ultimoEstudo = await _context.Estudos
                .Where(e => idsCiclo.Contains(e.AssuntoId))
                .OrderByDescending(e => e.DataTicks)
                .FirstOrDefaultAsync();

            // Se nunca houve estudo, retorna o 1º
            if (ultimoEstudo == null) return ciclo.First();

            // Identificar a ordem do assunto do último estudo
            var itemUltimo = ciclo.FirstOrDefault(c => c.AssuntoId == ultimoEstudo.AssuntoId);

            // Caso o assunto tenha sido removido do ciclo recentemente, fallback para o primeiro
            if (itemUltimo == null) return ciclo.First();

            // Retornar o assunto da Ordem + 1 (Loop)
            var proximaOrdem = itemUltimo.Ordem + 1;
            var proximo = ciclo.FirstOrDefault(c => c.Ordem == proximaOrdem);

            return proximo ?? ciclo.First();
        }

        public async Task AdicionarAoCicloAsync(int assuntoId, int minutos)
        {
            // Verificar se já existe
            var existe = await _context.Set<CicloEstudo>().AnyAsync(c => c.AssuntoId == assuntoId);
            if (existe) return;

            // Obter maior ordem atual
            var maxOrdem = await _context.Set<CicloEstudo>().MaxAsync(c => (int?)c.Ordem) ?? 0;

            var novoItem = new CicloEstudo
            {
                AssuntoId = assuntoId,
                Ordem = maxOrdem + 1,
                DuracaoMinutos = minutos
            };

            _context.Set<CicloEstudo>().Add(novoItem);
            await _context.SaveChangesAsync();
        }

        public async Task RemoverDoCicloAsync(int assuntoId)
        {
            var item = await _context.Set<CicloEstudo>().FirstOrDefaultAsync(c => c.AssuntoId == assuntoId);
            if (item == null) return;

            _context.Set<CicloEstudo>().Remove(item);
            await _context.SaveChangesAsync();

            // Reindexar para não deixar lacunas
            var ciclo = await _context.Set<CicloEstudo>().OrderBy(c => c.Ordem).ToListAsync();
            for (int i = 0; i < ciclo.Count; i++)
            {
                ciclo[i].Ordem = i + 1;
            }
            await _context.SaveChangesAsync();
        }

        public async Task MoverItemAsync(int assuntoId, bool paraCima)
        {
            var item = await _context.Set<CicloEstudo>().FirstOrDefaultAsync(c => c.AssuntoId == assuntoId);
            if (item == null) return;

            int ordemAtual = item.Ordem;
            int ordemDestino = paraCima ? ordemAtual - 1 : ordemAtual + 1;

            var itemTroca = await _context.Set<CicloEstudo>().FirstOrDefaultAsync(c => c.Ordem == ordemDestino);
            if (itemTroca == null) return; // Não há para onde mover (início ou fim da lista)

            // Swap
            item.Ordem = ordemDestino;
            itemTroca.Ordem = ordemAtual;

            await _context.SaveChangesAsync();
        }

        public async Task AtualizarDuracaoAsync(int assuntoId, int minutos)
        {
            var item = await _context.Set<CicloEstudo>().FirstOrDefaultAsync(c => c.AssuntoId == assuntoId);
            if (item != null)
            {
                item.DuracaoMinutos = minutos;
                await _context.SaveChangesAsync();
            }
        }

        // StudyMinder/Services/CicloEstudoService.cs

        public async Task<PagedResult<Assunto>> ObterAssuntosDisponiveisPaginadoAsync(int paginaAtual, int itensPorPagina, string searchText)
        {
            // 1. Query base: Busca todos os assuntos ativos e inclui a disciplina
            // Nota: Não aplicamos o filtro de texto aqui ainda
            var query = _context.Assuntos
                .Include(a => a.Disciplina)
                .Where(a => !a.Arquivado);

            List<Assunto> itemsDaPagina;
            int totalCount;

            // 2. Lógica de Pesquisa Híbrida
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // CENÁRIO A: Sem pesquisa (Paginação Otimizada no Banco)
                // Se não tem texto, contamos e paginamos direto no SQL para máxima performance
                totalCount = await query.CountAsync();

                itemsDaPagina = await query
                    .OrderBy(a => a.Nome)
                    .Skip((paginaAtual - 1) * itensPorPagina)
                    .Take(itensPorPagina)
                    .ToListAsync();
            }
            else
            {
                // CENÁRIO B: Com pesquisa (Filtro preciso em Memória)
                // SQLite não suporta nativamente ignorar acentos no 'Contains'.
                // Trazemos os dados para memória para usar o StringNormalizationHelper.

                var todosAssuntos = await query.ToListAsync();

                // Aplica o filtro C# que ignora Case e Acentos
                var listaFiltrada = todosAssuntos
                    .Where(a => StringNormalizationHelper.ContainsIgnoreCaseAndAccents(a.Nome, searchText) ||
                                StringNormalizationHelper.ContainsIgnoreCaseAndAccents(a.Disciplina.Nome, searchText))
                    .OrderBy(a => a.Nome)
                    .ToList();

                totalCount = listaFiltrada.Count;

                // Realiza a paginação na lista já filtrada em memória
                itemsDaPagina = listaFiltrada
                    .Skip((paginaAtual - 1) * itensPorPagina)
                    .Take(itensPorPagina)
                    .ToList();
            }

            return new PagedResult<Assunto>
            {
                Items = itemsDaPagina,
                TotalCount = totalCount,
                TotalItems = totalCount,
                PageNumber = paginaAtual,
                PageSize = itensPorPagina
            };
        }
    }
}
