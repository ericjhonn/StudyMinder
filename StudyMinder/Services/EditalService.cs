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
    public class EditalService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;

        public EditalService(StudyMinderContext context, AuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }

        public async Task AdicionarAsync(Edital edital)
        {
            _auditoriaService.AtualizarAuditoria(edital, true);
            _context.Editais.Add(edital);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Edital edital)
        {
            // DEBUG: Log dos valores recebidos
            System.Diagnostics.Debug.WriteLine($"[DEBUG] EditalService.AtualizarAsync() - ID: {edital.Id}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] BrancosProva recebido: {edital.BrancosProva}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] AnuladasProva recebido: {edital.AnuladasProva}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] TipoProvaId recebido: {edital.TipoProvaId}");

            var editalExistente = await _context.Editais.FindAsync(edital.Id);
            if (editalExistente == null) throw new KeyNotFoundException("Edital não encontrado");

            editalExistente.Cargo = edital.Cargo;
            editalExistente.Orgao = edital.Orgao;
            editalExistente.Salario = edital.Salario;
            editalExistente.VagasImediatas = edital.VagasImediatas;
            editalExistente.VagasCadastroReserva = edital.VagasCadastroReserva;
            editalExistente.Concorrencia = edital.Concorrencia;
            editalExistente.Colocacao = edital.Colocacao;
            editalExistente.NumeroInscricao = edital.NumeroInscricao;
            editalExistente.AcertosProva = edital.AcertosProva;
            editalExistente.ErrosProva = edital.ErrosProva;
            editalExistente.BrancosProva = edital.BrancosProva;
            editalExistente.AnuladasProva = edital.AnuladasProva;
            editalExistente.TipoProvaId = edital.TipoProvaId;
            editalExistente.EscolaridadeId = edital.EscolaridadeId;
            editalExistente.Banca = edital.Banca;
            editalExistente.Area = edital.Area;
            editalExistente.Link = edital.Link;
            editalExistente.ValorInscricao = edital.ValorInscricao;
            editalExistente.FaseEditalId = edital.FaseEditalId;
            editalExistente.ProvaDiscursiva = edital.ProvaDiscursiva;
            editalExistente.ProvaTitulos = edital.ProvaTitulos;
            editalExistente.ProvaTaf = edital.ProvaTaf;
            editalExistente.ProvaPratica = edital.ProvaPratica;
            editalExistente.DataAbertura = edital.DataAbertura;
            editalExistente.DataProva = edital.DataProva;
            editalExistente.Arquivado = edital.Arquivado;

            // DEBUG: Log dos valores atribuídos
            System.Diagnostics.Debug.WriteLine($"[DEBUG] BrancosProva atribuído: {editalExistente.BrancosProva}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] AnuladasProva atribuído: {editalExistente.AnuladasProva}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] TipoProvaId atribuído: {editalExistente.TipoProvaId}");

            _auditoriaService.AtualizarAuditoria(editalExistente, false);
            await _context.SaveChangesAsync();

            // DEBUG: Log após salvar
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Edital atualizado com sucesso. Valores no BD:");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] BrancosProva: {editalExistente.BrancosProva}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] AnuladasProva: {editalExistente.AnuladasProva}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] TipoProvaId: {editalExistente.TipoProvaId}");
        }

        public async Task<Edital?> ObterPorIdAsync(int id)
        {
            return await _context.Editais
                .Include(e => e.Escolaridade)
                .Include(e => e.FaseEdital)
                .Include(e => e.TiposProva)
                .Include(e => e.EditalAssuntos)
                    .ThenInclude(ea => ea.Assunto)
                .Include(e => e.EditalCronogramas)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<Edital>> ObterTodosAsync(bool incluirInativos = false)
        {
            var query = _context.Editais
                .Include(e => e.Escolaridade)
                .Include(e => e.FaseEdital)
                .Include(e => e.EditalAssuntos)
                .Include(e => e.EditalCronogramas)
                .AsQueryable();

            if (!incluirInativos)
            {
                query = query.Where(e => !e.Arquivado);
            }

            return await query
                .OrderByDescending(e => e.DataProvaTicks)
                .ToListAsync();
        }

        public async Task<PagedResult<Edital>> ObterPaginadoAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string? searchTerm = null,
            bool incluirInativos = false,
            int? faseEditalId = null)
        {
            var query = _context.Editais
                .Include(e => e.Escolaridade)
                .Include(e => e.FaseEdital)
                .Include(e => e.EditalAssuntos)
                    .ThenInclude(ea => ea.Assunto)
                        .ThenInclude(a => a.Estudos)
                .Include(e => e.EditalCronogramas)
                .AsQueryable();

            if (!incluirInativos)
            {
                query = query.Where(e => !e.Arquivado);
            }

            if (faseEditalId.HasValue)
            {
                query = query.Where(e => e.FaseEditalId == faseEditalId.Value);
            }

            // Carregar dados do banco (sem filtro de pesquisa)
            var allEditais = await query
                .OrderByDescending(e => e.DataProvaTicks)
                .ToListAsync();

            // Aplicar filtro de pesquisa em memória (case-insensitive e sem acentos)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                allEditais = allEditais.Where(e =>
                    StringNormalizationHelper.ContainsIgnoreCaseAndAccents(e.Cargo, searchTerm) ||
                    StringNormalizationHelper.ContainsIgnoreCaseAndAccents(e.Orgao, searchTerm) ||
                    (e.Banca != null && StringNormalizationHelper.ContainsIgnoreCaseAndAccents(e.Banca, searchTerm)) ||
                    (e.Area != null && StringNormalizationHelper.ContainsIgnoreCaseAndAccents(e.Area, searchTerm))
                ).ToList();
            }

            var totalCount = allEditais.Count;

            // Aplicar paginação
            var items = allEditais
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<Edital>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task ExcluirAsync(int id)
        {
            var edital = await _context.Editais
                .Include(e => e.EditalAssuntos)
                .Include(e => e.EditalCronogramas)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (edital == null) throw new KeyNotFoundException("Edital não encontrado");

            _context.Editais.Remove(edital);
            await _context.SaveChangesAsync();
        }

        public async Task DesativarAsync(int id)
        {
            var edital = await _context.Editais.FindAsync(id);
            if (edital == null) throw new KeyNotFoundException("Edital não encontrado");

            edital.Arquivado = true;
            _auditoriaService.AtualizarAuditoria(edital, false);
            await _context.SaveChangesAsync();
        }

        public async Task AtivarAsync(int id)
        {
            var edital = await _context.Editais.FindAsync(id);
            if (edital == null) throw new KeyNotFoundException("Edital não encontrado");

            edital.Arquivado = false;
            _auditoriaService.AtualizarAuditoria(edital, false);
            await _context.SaveChangesAsync();
        }

        public async Task AdicionarAssuntoAsync(int editalId, int assuntoId)
        {
            var editalAssunto = new EditalAssunto
            {
                EditalId = editalId,
                AssuntoId = assuntoId
            };

            _context.EditalAssuntos.Add(editalAssunto);
            await _context.SaveChangesAsync();
        }

        public async Task RemoverAssuntoAsync(int editalId, int assuntoId)
        {
            var editalAssunto = await _context.EditalAssuntos
                .FirstOrDefaultAsync(ea => ea.EditalId == editalId && ea.AssuntoId == assuntoId);

            if (editalAssunto == null) throw new KeyNotFoundException("Associação não encontrada");

            _context.EditalAssuntos.Remove(editalAssunto);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Assunto>> ObterAssuntosDoEditalAsync(int editalId)
        {
            return await _context.EditalAssuntos
                .Where(ea => ea.EditalId == editalId)
                .Select(ea => ea.Assunto)
                .ToListAsync();
        }

        /// <summary>
        /// Salva edital com assuntos vinculados e cronograma em uma única transação.
        /// </summary>
        public async Task SalvarEditalComRelacionamentosAsync(
            Edital edital, 
            List<int> assuntosIds, 
            List<EditalCronograma> cronogramaEventos)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Salvar ou atualizar edital
                    if (edital.Id == 0)
                    {
                        // Novo edital
                        _auditoriaService.AtualizarAuditoria(edital, true);
                        _context.Editais.Add(edital);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // Editar edital existente
                        var editalExistente = await _context.Editais.FindAsync(edital.Id);
                        if (editalExistente != null)
                        {
                            // DEBUG: Log dos valores recebidos
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] SalvarEditalComRelacionamentosAsync - Editar Edital ID: {edital.Id}");
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] BrancosProva recebido: {edital.BrancosProva}");
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] AnuladasProva recebido: {edital.AnuladasProva}");
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] TipoProvaId recebido: {edital.TipoProvaId}");

                            editalExistente.Cargo = edital.Cargo;
                            editalExistente.Orgao = edital.Orgao;
                            editalExistente.Salario = edital.Salario;
                            editalExistente.VagasImediatas = edital.VagasImediatas;
                            editalExistente.VagasCadastroReserva = edital.VagasCadastroReserva;
                            editalExistente.Concorrencia = edital.Concorrencia;
                            editalExistente.Colocacao = edital.Colocacao;
                            editalExistente.NumeroInscricao = edital.NumeroInscricao;
                            editalExistente.AcertosProva = edital.AcertosProva;
                            editalExistente.ErrosProva = edital.ErrosProva;
                            editalExistente.BrancosProva = edital.BrancosProva;
                            editalExistente.AnuladasProva = edital.AnuladasProva;
                            editalExistente.TipoProvaId = edital.TipoProvaId;
                            editalExistente.EscolaridadeId = edital.EscolaridadeId;
                            editalExistente.Banca = edital.Banca;
                            editalExistente.Area = edital.Area;
                            editalExistente.Link = edital.Link;
                            editalExistente.ValorInscricao = edital.ValorInscricao;
                            editalExistente.FaseEditalId = edital.FaseEditalId;
                            editalExistente.ProvaDiscursiva = edital.ProvaDiscursiva;
                            editalExistente.ProvaTitulos = edital.ProvaTitulos;
                            editalExistente.ProvaTaf = edital.ProvaTaf;
                            editalExistente.ProvaPratica = edital.ProvaPratica;
                            editalExistente.DataAbertura = edital.DataAbertura;
                            editalExistente.DataProva = edital.DataProva;
                            editalExistente.Arquivado = edital.Arquivado;
                            editalExistente.Validade = edital.Validade;
                            editalExistente.DataHomologacaoTicks = edital.DataHomologacaoTicks;
                            editalExistente.BoletoPago = edital.BoletoPago;

                            // DEBUG: Log dos valores atribuídos
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] BrancosProva atribuído: {editalExistente.BrancosProva}");
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] AnuladasProva atribuído: {editalExistente.AnuladasProva}");
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] TipoProvaId atribuído: {editalExistente.TipoProvaId}");

                            _auditoriaService.AtualizarAuditoria(editalExistente, false);
                            await _context.SaveChangesAsync();

                            // DEBUG: Log após salvar
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Edital salvo com sucesso. BrancosProva no BD: {editalExistente.BrancosProva}");
                        }
                    }

                    // 2. Atualizar assuntos vinculados
                    var editalAssuntosExistentes = await _context.EditalAssuntos
                        .Where(ea => ea.EditalId == edital.Id)
                        .ToListAsync();

                    // Remover assuntos que foram desvinculados
                    var assuntosParaRemover = editalAssuntosExistentes
                        .Where(ea => !assuntosIds.Contains(ea.AssuntoId))
                        .ToList();

                    foreach (var assunto in assuntosParaRemover)
                    {
                        _context.EditalAssuntos.Remove(assunto);
                    }

                    // Adicionar novos assuntos
                    var assuntosExistentesIds = editalAssuntosExistentes
                        .Select(ea => ea.AssuntoId)
                        .ToHashSet();

                    var novasSelecoes = assuntosIds.Where(id => !assuntosExistentesIds.Contains(id)).ToList();
                    foreach (var assuntoId in novasSelecoes)
                    {
                        _context.EditalAssuntos.Add(new EditalAssunto
                        {
                            EditalId = edital.Id,
                            AssuntoId = assuntoId
                        });
                    }

                    await _context.SaveChangesAsync();

                    // 3. Atualizar cronograma
                    var cronogramaExistente = await _context.EditalCronograma
                        .Where(ec => ec.EditalId == edital.Id)
                        .ToListAsync();

                    // Remover eventos que foram deletados
                    var eventosParaRemover = cronogramaExistente
                        .Where(e => !cronogramaEventos.Any(ce => ce.Id == e.Id))
                        .ToList();

                    foreach (var evento in eventosParaRemover)
                    {
                        _context.EditalCronograma.Remove(evento);
                    }

                    // Adicionar ou atualizar eventos
                    foreach (var evento in cronogramaEventos)
                    {
                        if (evento.Id == 0)
                        {
                            // Novo evento
                            evento.EditalId = edital.Id;
                            _auditoriaService.AtualizarAuditoria(evento, true);
                            _context.EditalCronograma.Add(evento);
                        }
                        else
                        {
                            // Atualizar evento existente
                            var eventoExistente = cronogramaExistente.FirstOrDefault(e => e.Id == evento.Id);
                            if (eventoExistente != null)
                            {
                                eventoExistente.Evento = evento.Evento;
                                eventoExistente.DataEvento = evento.DataEvento;
                                eventoExistente.Concluido = evento.Concluido;
                                eventoExistente.Ignorado = evento.Ignorado;
                                _auditoriaService.AtualizarAuditoria(eventoExistente, false);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Confirmar transação
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<List<Edital>> ObterEditaisPorAssuntoAsync(int assuntoId)
        {
            return await _context.EditalAssuntos
                .Where(ea => ea.AssuntoId == assuntoId)
                .Select(ea => ea.Edital)
                .Include(e => e.Escolaridade)
                .Include(e => e.FaseEdital)
                .ToListAsync();
        }
    }
}
