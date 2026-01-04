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
    public class DisciplinaService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;

        public DisciplinaService(StudyMinderContext context, AuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }

        public Task<bool> NomeExistsAsync(string nome, int? id = null)
        {
            if (id.HasValue)
            {
                return _context.Disciplinas.AnyAsync(d => d.Nome == nome && d.Id != id.Value);
            }
            return _context.Disciplinas.AnyAsync(d => d.Nome == nome);
        }

        public async Task<PagedResult<Disciplina>> ObterPaginadoAsync(int pageNumber, int pageSize, string? searchText = null, bool incluirArquivadas = false)
        {
            var query = _context.Disciplinas
                .Include(d => d.Assuntos)
                    .ThenInclude(a => a.Estudos)
                .AsQueryable();

            if (!incluirArquivadas)
            {
                query = query.Where(d => !d.Arquivado);
            }

            // Carregar todas as disciplinas primeiro (sem paginação)
            var allDisciplinas = await query.AsNoTracking().ToListAsync();

            // Aplicar filtro em memória (case-insensitive e sem acentos)
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                allDisciplinas = allDisciplinas
                    .Where(d => StringNormalizationHelper.ContainsIgnoreCaseAndAccents(d.Nome, searchText))
                    .ToList();
            }

            var totalCount = allDisciplinas.Count;

            // Aplicar paginação
            var items = allDisciplinas
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<Disciplina> { Items = items, TotalCount = totalCount };
        }

        public async Task AdicionarAsync(Disciplina disciplina)
        {
            // Verificar nome único
            if (await _context.Disciplinas.AnyAsync(d => d.Nome == disciplina.Nome))
            {
                throw new InvalidOperationException("Já existe uma disciplina com este nome.");
            }

            _auditoriaService.AtualizarAuditoria(disciplina, true);
            _context.Disciplinas.Add(disciplina);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Disciplina disciplina)
        {
            // Verificar se a disciplina existe
            var disciplinaExistente = await _context.Disciplinas
                .FirstOrDefaultAsync(d => d.Id == disciplina.Id);
            
            if (disciplinaExistente == null)
            {
                throw new KeyNotFoundException("Disciplina não encontrada.");
            }

            // Verificar nome único
            if (await _context.Disciplinas.AnyAsync(d => d.Nome == disciplina.Nome && d.Id != disciplina.Id))
            {
                throw new InvalidOperationException("Já existe outra disciplina com este nome.");
            }

            // Atualizar propriedades
            disciplinaExistente.Nome = disciplina.Nome;
            disciplinaExistente.Cor = disciplina.Cor;
            _auditoriaService.AtualizarAuditoria(disciplinaExistente, false);
            
            await _context.SaveChangesAsync();
        }

        public async Task ArquivarAsync(int id)
        {
            var disciplina = await _context.Disciplinas
                .Include(d => d.Assuntos)
                    .ThenInclude(a => a.Estudos)
                .FirstOrDefaultAsync(d => d.Id == id);
                
            if (disciplina == null) throw new KeyNotFoundException("Disciplina não encontrada");
            
            disciplina.Arquivado = true;
            _auditoriaService.AtualizarAuditoria(disciplina, false);
            
            // Arquivar assuntos relacionados
            foreach (var assunto in disciplina.Assuntos)
            {
                assunto.Arquivado = true;
                _auditoriaService.AtualizarAuditoria(assunto, false);
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task ExcluirAsync(int id)
        {
            var disciplina = await _context.Disciplinas
                .Include(d => d.Assuntos)
                    .ThenInclude(a => a.Estudos)
                .FirstOrDefaultAsync(d => d.Id == id);
                
            if (disciplina == null) throw new KeyNotFoundException("Disciplina não encontrada");
            
            // Verificar se há assuntos vinculados
            if (disciplina.Assuntos.Any(a => !a.Arquivado))
            {
                throw new InvalidOperationException(
                    "Não é possível excluir uma disciplina com assuntos ativos. " +
                    "Arquive a disciplina primeiro.");
            }
            
            _context.Disciplinas.Remove(disciplina);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarProgressoAsync(int disciplinaId)
        {
            var disciplina = await _context.Disciplinas
                .Include(d => d.Assuntos)
                    .ThenInclude(a => a.Estudos)
                .FirstOrDefaultAsync(d => d.Id == disciplinaId);
                
            if (disciplina == null) 
                throw new KeyNotFoundException("Disciplina não encontrada");
            
            // Invalida o cache para forçar o recálculo
            disciplina.InvalidateProgressCache();
            
            // Força o cálculo do progresso e atualiza a data de modificação
            var progresso = disciplina.Progresso;
            disciplina.AtualizarDataModificacao();
            
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarRendimentoAsync(int disciplinaId)
        {
            var disciplina = await _context.Disciplinas
                .Include(d => d.Assuntos)
                    .ThenInclude(a => a.Estudos)
                .FirstOrDefaultAsync(d => d.Id == disciplinaId);
                
            if (disciplina == null) throw new KeyNotFoundException("Disciplina não encontrada");
            
            // Forçar recálculo na próxima leitura
            disciplina.AtualizarDataModificacao();
            await _context.SaveChangesAsync();
        }

        public async Task<List<Disciplina>> ObterTodasAsync(bool incluirArquivadas = false)
        {
            var query = _context.Disciplinas
                .Include(d => d.Assuntos)
                    .ThenInclude(a => a.Estudos)
                .AsQueryable();

            if (!incluirArquivadas)
            {
                query = query.Where(d => !d.Arquivado);
            }

            return await query
                .OrderBy(d => d.Nome)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
