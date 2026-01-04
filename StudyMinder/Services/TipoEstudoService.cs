using StudyMinder.Models;
using StudyMinder.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudyMinder.Services
{
    public class TipoEstudoService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;

        public TipoEstudoService(StudyMinderContext context, AuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }

        public async Task AdicionarAsync(TipoEstudo tipoEstudo)
        {
            // Verificar nome único
            if (await _context.TiposEstudo.AnyAsync(t => t.Nome == tipoEstudo.Nome))
            {
                throw new InvalidOperationException("Já existe um tipo de estudo com este nome.");
            }

            _auditoriaService.AtualizarAuditoria(tipoEstudo, true);
            _context.TiposEstudo.Add(tipoEstudo);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(TipoEstudo tipoEstudo)
        {
            // Verificar nome único
            if (await _context.TiposEstudo.AnyAsync(t => t.Nome == tipoEstudo.Nome && t.Id != tipoEstudo.Id))
            {
                throw new InvalidOperationException("Já existe outro tipo de estudo com este nome.");
            }

            var tipoExistente = await _context.TiposEstudo.FindAsync(tipoEstudo.Id);
            if (tipoExistente == null) throw new KeyNotFoundException("Tipo de estudo não encontrado");

            tipoExistente.Nome = tipoEstudo.Nome;
            tipoExistente.Ativo = tipoEstudo.Ativo;

            _auditoriaService.AtualizarAuditoria(tipoExistente, false);
            await _context.SaveChangesAsync();
        }

        public async Task<TipoEstudo?> ObterPorIdAsync(int id)
        {
            return await _context.TiposEstudo
                .Include(t => t.Estudos)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<TipoEstudo>> ObterTodosAsync(bool incluirInativos = false)
        {
            var query = _context.TiposEstudo.AsQueryable();

            if (!incluirInativos)
            {
                query = query.Where(t => t.Ativo);
            }

            return await query
                .OrderBy(t => t.Nome)
                .ToListAsync();
        }

        public async Task<List<TipoEstudo>> ObterAtivosAsync()
        {
            return await _context.TiposEstudo
                .Where(t => t.Ativo)
                .OrderBy(t => t.Nome)
                .ToListAsync();
        }

        public async Task DesativarAsync(int id)
        {
            var tipoEstudo = await _context.TiposEstudo
                .Include(t => t.Estudos)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipoEstudo == null) throw new KeyNotFoundException("Tipo de estudo não encontrado");

            // Verificar se há estudos vinculados
            if (tipoEstudo.Estudos.Any())
            {
                throw new InvalidOperationException(
                    "Não é possível desativar um tipo de estudo que possui estudos vinculados.");
            }

            tipoEstudo.Ativo = false;
            _auditoriaService.AtualizarAuditoria(tipoEstudo, false);
            await _context.SaveChangesAsync();
        }

        public async Task AtivarAsync(int id)
        {
            var tipoEstudo = await _context.TiposEstudo.FindAsync(id);
            if (tipoEstudo == null) throw new KeyNotFoundException("Tipo de estudo não encontrado");

            tipoEstudo.Ativo = true;
            _auditoriaService.AtualizarAuditoria(tipoEstudo, false);
            await _context.SaveChangesAsync();
        }

        public async Task ExcluirAsync(int id)
        {
            var tipoEstudo = await _context.TiposEstudo
                .Include(t => t.Estudos)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipoEstudo == null) throw new KeyNotFoundException("Tipo de estudo não encontrado");

            // Verificar se há estudos vinculados
            if (tipoEstudo.Estudos.Any())
            {
                throw new InvalidOperationException(
                    "Não é possível excluir um tipo de estudo que possui estudos vinculados. " +
                    "Considere desativá-lo ao invés de excluí-lo.");
            }

            _context.TiposEstudo.Remove(tipoEstudo);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> NomeExisteAsync(string nome, int? id = null)
        {
            if (id.HasValue)
            {
                return await _context.TiposEstudo.AnyAsync(t => t.Nome == nome && t.Id != id.Value);
            }
            return await _context.TiposEstudo.AnyAsync(t => t.Nome == nome);
        }
    }
}
