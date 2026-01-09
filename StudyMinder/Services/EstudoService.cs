using StudyMinder.Models;
using StudyMinder.Data;
using StudyMinder.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudyMinder.Services
{
    public class EstudoService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;

        public EstudoService(StudyMinderContext context, AuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }

        public async Task AdicionarAsync(Estudo estudo)
        {
            _auditoriaService.AtualizarAuditoria(estudo, true);
            _context.Estudos.Add(estudo);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Estudo estudo)
        {
            var estudoExistente = await _context.Estudos.FindAsync(estudo.Id);
            if (estudoExistente == null) throw new KeyNotFoundException("Estudo não encontrado");

            estudoExistente.TipoEstudoId = estudo.TipoEstudoId;
            estudoExistente.AssuntoId = estudo.AssuntoId;
            estudoExistente.Data = estudo.Data;
            estudoExistente.DuracaoTicks = estudo.DuracaoTicks;
            estudoExistente.Acertos = estudo.Acertos;
            estudoExistente.Erros = estudo.Erros;
            estudoExistente.PaginaInicial = estudo.PaginaInicial;
            estudoExistente.PaginaFinal = estudo.PaginaFinal;
            estudoExistente.Material = estudo.Material;
            estudoExistente.Professor = estudo.Professor;
            estudoExistente.Topicos = estudo.Topicos;
            estudoExistente.Comentarios = estudo.Comentarios;

            _auditoriaService.AtualizarAuditoria(estudoExistente, false);
            await _context.SaveChangesAsync();
        }

        public async Task<Estudo?> ObterPorIdAsync(int id)
        {
            return await _context.Estudos
                .Include(e => e.TipoEstudo)
                .Include(e => e.Assunto)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<Estudo>> ObterPorAssuntoAsync(int assuntoId)
        {
            return await _context.Estudos
                .Include(e => e.TipoEstudo)
                .Where(e => e.AssuntoId == assuntoId)
                .OrderByDescending(e => e.Data)
                .ToListAsync();
        }

        public async Task<List<Estudo>> ObterPorDisciplinaAsync(int disciplinaId)
        {
            return await _context.Estudos
                .Include(e => e.TipoEstudo)
                .Include(e => e.Assunto)
                .Where(e => e.Assunto.DisciplinaId == disciplinaId)
                .OrderByDescending(e => e.Data)
                .ToListAsync();
        }

        public async Task<PagedResult<Estudo>> ObterPaginadoAsync(
            int pageNumber = 1,
            int pageSize = 10,
            int? assuntoId = null,
            int? disciplinaId = null,
            int? tipoEstudoId = null,
            DateTime? dataInicio = null,
            DateTime? dataFim = null,
            bool filtrarAssuntosConcluidos = false)
        {
            var query = _context.Estudos
                .Include(e => e.TipoEstudo)
                .Include(e => e.Assunto)
                    .ThenInclude(a => a.Disciplina)
                    .OrderBy(e => e.Id)
                .AsQueryable();

            if (assuntoId.HasValue)
            {
                query = query.Where(e => e.AssuntoId == assuntoId.Value);
            }

            if (disciplinaId.HasValue)
            {
                query = query.Where(e => e.Assunto.DisciplinaId == disciplinaId.Value);
            }

            if (dataInicio.HasValue)
            {
                var dataInicioTicks = dataInicio.Value.Ticks;
                query = query.Where(e => e.DataTicks >= dataInicioTicks);
            }

            if (dataFim.HasValue)
            {
                var dataFimTicks = dataFim.Value.Ticks;
                query = query.Where(e => e.DataTicks <= dataFimTicks);
            }

            if (filtrarAssuntosConcluidos)
            {
                query = query.Where(e => e.Assunto.Concluido);
            }

            if (tipoEstudoId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ObterPaginadoAsync aplicando filtro TipoEstudoId: {tipoEstudoId.Value}");
                query = query.Where(e => e.TipoEstudoId == tipoEstudoId.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(e => e.DataTicks)
                .ThenBy(e => e.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Estudo>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Pesquisa otimizada por assunto e disciplina com suporte a case-insensitive e sem acentos
        /// Para melhor performance, considere criar índices:
        /// - CREATE INDEX IX_Assuntos_Nome ON Assuntos (Nome)
        /// - CREATE INDEX IX_Disciplinas_Nome ON Disciplinas (Nome)
        /// - CREATE INDEX IX_Estudos_DataTicks ON Estudos (DataTicks)
        /// </summary>
        public async Task<PagedResult<Estudo>> PesquisarAsync(
            string searchText,
            int pageNumber = 1,
            int pageSize = 10,
            int? assuntoId = null,
            int? disciplinaId = null,
            DateTime? dataInicio = null,
            DateTime? dataFim = null,
            bool filtrarAssuntosConcluidos = false,
            int? tipoEstudoId = null)
        {
            var query = _context.Estudos
                .Include(e => e.TipoEstudo)
                .Include(e => e.Assunto)
                    .ThenInclude(a => a.Disciplina)
                .AsQueryable();

            // Aplicar filtros que podem ser traduzidos para SQL
            if (assuntoId.HasValue)
            {
                query = query.Where(e => e.AssuntoId == assuntoId.Value);
            }

            if (disciplinaId.HasValue)
            {
                query = query.Where(e => e.Assunto.DisciplinaId == disciplinaId.Value);
            }

            if (dataInicio.HasValue)
            {
                var dataInicioTicks = dataInicio.Value.Ticks;
                query = query.Where(e => e.DataTicks >= dataInicioTicks);
            }

            if (dataFim.HasValue)
            {
                var dataFimTicks = dataFim.Value.Ticks;
                query = query.Where(e => e.DataTicks <= dataFimTicks);
            }

            if (filtrarAssuntosConcluidos)
            {
                query = query.Where(e => e.Assunto.Concluido);
            }

            if (tipoEstudoId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Aplicando filtro TipoEstudoId: {tipoEstudoId.Value}");
                query = query.Where(e => e.TipoEstudoId == tipoEstudoId.Value);
            }

            // Carregar dados do banco (sem filtro de pesquisa)
            var allItems = await query
                .OrderByDescending(e => e.DataTicks)
                .ToListAsync();
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] PesquisarAsync retornou {allItems.Count} itens");

            // Aplicar filtro de pesquisa em memória (case-insensitive e sem acentos)
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                System.Diagnostics.Debug.WriteLine($"Aplicando filtro de pesquisa otimizado: '{searchText}'");
                allItems = allItems.Where(e =>
                    StringNormalizationHelper.ContainsIgnoreCaseAndAccents(e.Assunto.Nome, searchText) ||
                    StringNormalizationHelper.ContainsIgnoreCaseAndAccents(e.Assunto.Disciplina.Nome, searchText)
                ).ToList();
            }

            var totalCount = allItems.Count;

            // Aplicar paginação
            var items = allItems
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<Estudo>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task ExcluirAsync(int id)
        {
            var estudo = await _context.Estudos
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estudo == null)
                throw new KeyNotFoundException("Estudo não encontrado");

            _context.Estudos.Remove(estudo);
            await _context.SaveChangesAsync();
        }

        public async Task<double> CalcularHorasTotaisAsync(int? assuntoId = null, int? disciplinaId = null)
        {
            var query = _context.Estudos.AsQueryable();

            if (assuntoId.HasValue)
            {
                query = query.Where(e => e.AssuntoId == assuntoId.Value);
            }

            if (disciplinaId.HasValue)
            {
                query = query.Where(e => e.Assunto.DisciplinaId == disciplinaId.Value);
            }

            var estudos = await query.ToListAsync();
            return estudos.Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours);
        }

        public async Task<Dictionary<string, int>> ObterEstatisticasAsync(int? assuntoId = null, int? disciplinaId = null)
        {
            var query = _context.Estudos.AsQueryable();

            if (assuntoId.HasValue)
            {
                query = query.Where(e => e.AssuntoId == assuntoId.Value);
            }

            if (disciplinaId.HasValue)
            {
                query = query.Where(e => e.Assunto.DisciplinaId == disciplinaId.Value);
            }

            var estudos = await query.ToListAsync();

            return new Dictionary<string, int>
            {
                { "TotalEstudos", estudos.Count },
                { "TotalAcertos", estudos.Sum(e => e.Acertos) },
                { "TotalErros", estudos.Sum(e => e.Erros) },
                { "TotalQuestoes", estudos.Sum(e => e.Acertos + e.Erros) },
                { "TotalPaginas", estudos.Sum(e => e.PaginaFinal - e.PaginaInicial) }
            };
        }

        /// <summary>
        /// Obtém um tipo de estudo pelo nome.
        /// </summary>
        public async Task<TipoEstudo?> ObterTipoEstudoPorNomeAsync(string nome)
        {
            return await _context.TiposEstudo
                .FirstOrDefaultAsync(t => t.Nome == nome);
        }

        /// <summary>
        /// Obtém todos os estudos dentro de um intervalo de datas, ordenados por data e ID.
        /// </summary>
        public async Task<List<Estudo>> ObterPorIntervaloDataAsync(DateTime dataInicio, DateTime dataFim)
        {
            var dataInicioTicks = dataInicio.Ticks;
            var dataFimTicks = dataFim.Ticks;

            return await _context.Estudos
                .Where(e => e.DataTicks >= dataInicioTicks && e.DataTicks <= dataFimTicks)
                .OrderBy(e => e.DataTicks)
                .ThenBy(e => e.Id)
                .ToListAsync();
        }

        public async Task<Estudo?> ObterUltimoEstudoPorAssuntoAsync(int assuntoId)
        {
            return await _context.Estudos
                .Where(e => e.AssuntoId == assuntoId)
                .OrderByDescending(e => e.DataTicks)
                .FirstOrDefaultAsync();
        }
    }
}
