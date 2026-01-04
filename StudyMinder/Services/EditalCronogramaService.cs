using StudyMinder.Models;
using StudyMinder.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudyMinder.Services
{
    public class EditalCronogramaService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;

        public EditalCronogramaService(StudyMinderContext context, AuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }

        public async Task AdicionarAsync(EditalCronograma cronograma)
        {
            _auditoriaService.AtualizarAuditoria(cronograma, true);
            _context.EditalCronograma.Add(cronograma);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(EditalCronograma cronograma)
        {
            var cronogramaExistente = await _context.EditalCronograma.FindAsync(cronograma.Id);
            if (cronogramaExistente == null) throw new KeyNotFoundException("Cronograma não encontrado");

            cronogramaExistente.EditalId = cronograma.EditalId;
            cronogramaExistente.Evento = cronograma.Evento;
            cronogramaExistente.DataEvento = cronograma.DataEvento;
            cronogramaExistente.Concluido = cronograma.Concluido;
            cronogramaExistente.Ignorado = cronograma.Ignorado;

            _auditoriaService.AtualizarAuditoria(cronogramaExistente, false);
            await _context.SaveChangesAsync();
        }

        public async Task<EditalCronograma?> ObterPorIdAsync(int id)
        {
            return await _context.EditalCronograma
                .Include(c => c.Edital)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<EditalCronograma>> ObterPorEditalAsync(int editalId, bool incluirIgnorados = false)
        {
            var query = _context.EditalCronograma
                .Where(c => c.EditalId == editalId);

            if (!incluirIgnorados)
            {
                query = query.Where(c => !c.Ignorado);
            }

            return await query
                .OrderBy(c => c.DataEventoTicks)
                .ToListAsync();
        }

        public async Task<List<EditalCronograma>> ObterProximosEventosAsync(DateTime? dataLimite = null, bool incluirConcluidos = false)
        {
            var dataReferencia = dataLimite ?? DateTime.Now;
            var dataReferenciaTicks = dataReferencia.Ticks;

            var query = _context.EditalCronograma
                .Include(c => c.Edital)
                .Where(c => c.DataEventoTicks >= dataReferenciaTicks && !c.Ignorado);

            if (!incluirConcluidos)
            {
                query = query.Where(c => !c.Concluido);
            }

            return await query
                .OrderBy(c => c.DataEventoTicks)
                .ToListAsync();
        }

        public async Task<List<EditalCronograma>> ObterEventosPendentesAsync()
        {
            return await _context.EditalCronograma
                .Include(c => c.Edital)
                .Where(c => !c.Concluido && !c.Ignorado)
                .OrderBy(c => c.DataEventoTicks)
                .ToListAsync();
        }

        public async Task MarcarComoConcluidoAsync(int id)
        {
            var cronograma = await _context.EditalCronograma.FindAsync(id);
            if (cronograma == null) throw new KeyNotFoundException("Cronograma não encontrado");

            cronograma.Concluido = true;
            _auditoriaService.AtualizarAuditoria(cronograma, false);
            await _context.SaveChangesAsync();
        }

        public async Task MarcarComoNaoConcluidoAsync(int id)
        {
            var cronograma = await _context.EditalCronograma.FindAsync(id);
            if (cronograma == null) throw new KeyNotFoundException("Cronograma não encontrado");

            cronograma.Concluido = false;
            _auditoriaService.AtualizarAuditoria(cronograma, false);
            await _context.SaveChangesAsync();
        }

        public async Task IgnorarAsync(int id)
        {
            var cronograma = await _context.EditalCronograma.FindAsync(id);
            if (cronograma == null) throw new KeyNotFoundException("Cronograma não encontrado");

            cronograma.Ignorado = true;
            _auditoriaService.AtualizarAuditoria(cronograma, false);
            await _context.SaveChangesAsync();
        }

        public async Task DesignorarAsync(int id)
        {
            var cronograma = await _context.EditalCronograma.FindAsync(id);
            if (cronograma == null) throw new KeyNotFoundException("Cronograma não encontrado");

            cronograma.Ignorado = false;
            _auditoriaService.AtualizarAuditoria(cronograma, false);
            await _context.SaveChangesAsync();
        }

        public async Task ExcluirAsync(int id)
        {
            var cronograma = await _context.EditalCronograma.FindAsync(id);
            if (cronograma == null) throw new KeyNotFoundException("Cronograma não encontrado");

            _context.EditalCronograma.Remove(cronograma);
            await _context.SaveChangesAsync();
        }

        public async Task<int> ContarEventosPendentesAsync(int editalId)
        {
            return await _context.EditalCronograma
                .CountAsync(c => c.EditalId == editalId && !c.Concluido && !c.Ignorado);
        }

        public async Task<int> ContarEventosConcluidosAsync(int editalId)
        {
            return await _context.EditalCronograma
                .CountAsync(c => c.EditalId == editalId && c.Concluido);
        }
    }
}
