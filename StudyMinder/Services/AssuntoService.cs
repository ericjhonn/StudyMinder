using StudyMinder.Models;
using StudyMinder.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudyMinder.Services
{
    public class AssuntoService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;
        private readonly DisciplinaService _disciplinaService;

        public AssuntoService(StudyMinderContext context, AuditoriaService auditoriaService, DisciplinaService disciplinaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
            _disciplinaService = disciplinaService;
        }

        public async Task AdicionarAsync(Assunto assunto)
        {
            // Verificar nome único na disciplina
            if (await _context.Assuntos.AnyAsync(a => 
                a.Nome == assunto.Nome && 
                a.DisciplinaId == assunto.DisciplinaId))
            {
                throw new InvalidOperationException("Já existe um assunto com este nome nesta disciplina.");
            }
            
            _auditoriaService.AtualizarAuditoria(assunto, true);
            _context.Assuntos.Add(assunto);
            await _context.SaveChangesAsync();
            
            // Atualizar disciplina
            await _disciplinaService.AtualizarProgressoAsync(assunto.DisciplinaId);
        }

        public async Task AtualizarAsync(Assunto assunto)
        {
            // Verificar nome único na disciplina
            if (await _context.Assuntos.AnyAsync(a => 
                a.Nome == assunto.Nome && 
                a.DisciplinaId == assunto.DisciplinaId &&
                a.Id != assunto.Id))
            {
                throw new InvalidOperationException("Já existe outro assunto com este nome nesta disciplina.");
            }
            
            var assuntoExistente = await _context.Assuntos.FindAsync(assunto.Id);
            if (assuntoExistente == null) throw new KeyNotFoundException("Assunto não encontrado");
            
            // Impedir marcação como concluído se arquivado
            if (assunto.Concluido && assuntoExistente.Arquivado)
            {
                throw new InvalidOperationException("Não é possível marcar um assunto arquivado como concluído.");
            }
            
            assuntoExistente.Nome = assunto.Nome;
            assuntoExistente.CadernoQuestoes = assunto.CadernoQuestoes;
            assuntoExistente.Concluido = assunto.Concluido;
            assuntoExistente.Arquivado = assunto.Arquivado;
            
            if (assuntoExistente.Concluido)
            {
                assuntoExistente.MarcarComoConcluido();
            }
            else
            {
                assuntoExistente.MarcarComoNaoConcluido();
            }
            
            _auditoriaService.AtualizarAuditoria(assuntoExistente, false);
            await _context.SaveChangesAsync();
            
            // Atualizar disciplina
            await _disciplinaService.AtualizarProgressoAsync(assuntoExistente.DisciplinaId);
            await _disciplinaService.AtualizarRendimentoAsync(assuntoExistente.DisciplinaId);
        }

        // Métodos RegistrarAcerto e RegistrarErro removidos
        // Acertos e erros agora são registrados diretamente nos Estudos

        public async Task ArquivarAsync(int id)
        {
            var assunto = await _context.Assuntos.FindAsync(id);
            if (assunto == null) throw new KeyNotFoundException("Assunto não encontrado");
            
            assunto.Arquivado = true;
            
            if (assunto.Concluido)
            {
                assunto.MarcarComoNaoConcluido();
            }
            
            _auditoriaService.AtualizarAuditoria(assunto, false);
            await _context.SaveChangesAsync();
            
            // Atualizar disciplina
            await _disciplinaService.AtualizarProgressoAsync(assunto.DisciplinaId);
        }

        public async Task<Assunto?> ObterPorIdAsync(int id)
        {
            return await _context.Assuntos
                .Include(a => a.Estudos)
                .Include(a => a.EditalAssuntos)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Assunto>> ObterTodosAsync(bool incluirArquivados = false)
        {
            var query = _context.Assuntos
                .Include(a => a.Disciplina)
                .Include(a => a.Estudos)
                .AsQueryable();

            if (!incluirArquivados)
            {
                query = query.Where(a => !a.Arquivado);
            }

            return await query
                .OrderBy(a => a.Disciplina.Nome)
                .ThenBy(a => a.Nome)
                .ToListAsync();
        }

        public async Task<List<Assunto>> ObterPorDisciplinaAsync(int disciplinaId, bool incluirArquivados = false)
        {
            var query = _context.Assuntos
                .Include(a => a.Estudos)
                .Where(a => a.DisciplinaId == disciplinaId);

            if (!incluirArquivados)
            {
                query = query.Where(a => !a.Arquivado);
            }

            return await query
                .OrderBy(a => a.Nome)
                .ToListAsync();
        }

        public async Task<PagedResult<Assunto>> ObterPaginadoAsync(
            int disciplinaId,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            bool incluirArquivados = false)
        {
            var query = _context.Assuntos
                .Include(a => a.Estudos)
                .Where(a => a.DisciplinaId == disciplinaId);

            if (!incluirArquivados)
            {
                query = query.Where(a => !a.Arquivado);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a => a.Nome.Contains(searchTerm) || 
                    (a.CadernoQuestoes != null && a.CadernoQuestoes.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(a => a.Nome)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Assunto>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task ExcluirAsync(int id)
        {
            var assunto = await _context.Assuntos
                .Include(a => a.Estudos)
                .Include(a => a.EditalAssuntos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assunto == null) throw new KeyNotFoundException("Assunto não encontrado");

            // Verificar se há estudos vinculados
            if (assunto.Estudos.Any())
            {
                throw new InvalidOperationException(
                    "Não é possível excluir um assunto que possui estudos vinculados. " +
                    "Considere arquivá-lo ao invés de excluí-lo.");
            }

            _context.Assuntos.Remove(assunto);
            await _context.SaveChangesAsync();

            // Atualizar disciplina
            await _disciplinaService.AtualizarProgressoAsync(assunto.DisciplinaId);
        }

        public async Task<bool> NomeExisteAsync(string nome, int disciplinaId, int? assuntoId = null)
        {
            if (assuntoId.HasValue)
            {
                return await _context.Assuntos.AnyAsync(a =>
                    a.Nome == nome &&
                    a.DisciplinaId == disciplinaId &&
                    a.Id != assuntoId.Value);
            }

            return await _context.Assuntos.AnyAsync(a =>
                a.Nome == nome &&
                a.DisciplinaId == disciplinaId);
        }

        /// <summary>
        /// Move um assunto e todos os seus registros de estudo para outra disciplina.
        /// Esta operação é atômica e será revertida em caso de erro.
        /// </summary>
        public async Task MoverParaDisciplinaAsync(int assuntoId, int novaDisciplinaId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var assunto = await _context.Assuntos
                    .Include(a => a.Estudos)
                    .Include(a => a.EditalAssuntos)
                    .FirstOrDefaultAsync(a => a.Id == assuntoId);

                if (assunto == null)
                    throw new KeyNotFoundException("Assunto não encontrado");

                var novaDisciplina = await _context.Disciplinas.FindAsync(novaDisciplinaId);
                if (novaDisciplina == null)
                    throw new KeyNotFoundException("Disciplina de destino não encontrada");

                if (novaDisciplina.Arquivado)
                    throw new InvalidOperationException("Não é possível mover assunto para uma disciplina arquivada");

                // Verificar se já existe assunto com mesmo nome na disciplina destino
                if (await NomeExisteAsync(assunto.Nome, novaDisciplinaId))
                {
                    throw new InvalidOperationException(
                        $"Já existe um assunto com o nome '{assunto.Nome}' na disciplina de destino.");
                }

                var disciplinaAntigaId = assunto.DisciplinaId;

                // Mover o assunto
                assunto.DisciplinaId = novaDisciplinaId;
                assunto.AtualizarDataModificacao();

                // Nota: Estudos não têm DisciplinaId, apenas AssuntoId
                // O relacionamento é mantido automaticamente

                await _context.SaveChangesAsync();

                // Atualizar estatísticas de ambas as disciplinas
                await _disciplinaService.AtualizarProgressoAsync(disciplinaAntigaId);
                await _disciplinaService.AtualizarRendimentoAsync(disciplinaAntigaId);
                await _disciplinaService.AtualizarProgressoAsync(novaDisciplinaId);
                await _disciplinaService.AtualizarRendimentoAsync(novaDisciplinaId);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Calcula as horas estudadas de um assunto com base nos estudos.
        /// </summary>
        public async Task<double> CalcularHorasEstudadasAsync(int assuntoId)
        {
            var estudos = await _context.Estudos
                .Where(e => e.AssuntoId == assuntoId)
                .ToListAsync();

            return estudos.Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours);
        }

        // Nota: HorasEstudadas é uma propriedade calculada no modelo Assunto.cs
        // usando Estudos.Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours)
    }
}
