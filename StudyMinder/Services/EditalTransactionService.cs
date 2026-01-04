using StudyMinder.Models;
using StudyMinder.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudyMinder.Services
{
    /// <summary>
    /// Serviço para gerenciar transações de salvamento de editais atomicamente.
    /// Otimiza performance usando uma única transação para múltiplas operações.
    /// </summary>
    public class EditalTransactionService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;

        public EditalTransactionService(StudyMinderContext context, AuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }

        /// <summary>
        /// Salva um edital com todas as operações relacionadas em uma única transação atômica.
        /// Inclui: salvar edital, vincular/remover assuntos, atualizar cronograma.
        /// </summary>
        public async Task SalvarEditalComAssuntosAsync(
            Edital edital,
            bool isNovoEdital,
            List<Assunto> assuntosParaVincular,
            List<Assunto> assuntosParaRemover,
            List<EditalCronograma> cronogramaParaAtualizar)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔵 EditalTransactionService.SalvarEditalComAssuntosAsync() INICIADO");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] 📊 Edital ID={edital.Id}, Novo={isNovoEdital}, Assuntos Vincular={assuntosParaVincular.Count}, Assuntos Remover={assuntosParaRemover.Count}, Cronograma={cronogramaParaAtualizar.Count}");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Salvar edital
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 📝 Salvando edital");
                if (isNovoEdital)
                {
                    _auditoriaService.AtualizarAuditoria(edital, true);
                    _context.Editais.Add(edital);
                }
                else
                {
                    var editalExistente = await _context.Editais.FindAsync(edital.Id);
                    if (editalExistente == null)
                    {
                        throw new KeyNotFoundException("Edital não encontrado.");
                    }

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
                    editalExistente.BoletoPago = edital.BoletoPago;
                    editalExistente.DataAberturaTicks = edital.DataAberturaTicks;
                    editalExistente.DataProvaTicks = edital.DataProvaTicks;
                    editalExistente.Arquivado = edital.Arquivado;

                    _auditoriaService.AtualizarAuditoria(editalExistente, false);
                }

                // 1.5. Salvar edital PRIMEIRO para obter o ID (necessário para as relações)
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💾 Salvando edital para obter ID");
                await _context.SaveChangesAsync();

                // 2. Remover assuntos desvinculados
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🗑️ Removendo {assuntosParaRemover.Count} assuntos desvinculados");
                foreach (var assunto in assuntosParaRemover)
                {
                    var editalAssunto = await _context.EditalAssuntos
                        .FirstOrDefaultAsync(ea => ea.EditalId == edital.Id && ea.AssuntoId == assunto.Id);
                    
                    if (editalAssunto != null)
                    {
                        _context.EditalAssuntos.Remove(editalAssunto);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Assunto {assunto.Id} desvinculado do edital");
                    }
                }

                // 3. Vincular novos assuntos
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔗 Vinculando {assuntosParaVincular.Count} novos assuntos");
                foreach (var assunto in assuntosParaVincular)
                {
                    var editalAssuntoExistente = await _context.EditalAssuntos
                        .FirstOrDefaultAsync(ea => ea.EditalId == edital.Id && ea.AssuntoId == assunto.Id);
                    
                    if (editalAssuntoExistente == null)
                    {
                        _context.EditalAssuntos.Add(new EditalAssunto 
                        { 
                            EditalId = edital.Id, 
                            AssuntoId = assunto.Id 
                        });
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Assunto {assunto.Id} vinculado ao edital");
                    }
                }

                // 4. Atualizar cronograma
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 📅 Atualizando {cronogramaParaAtualizar.Count} eventos do cronograma");
                foreach (var evento in cronogramaParaAtualizar)
                {
                    var eventoExistente = await _context.EditalCronograma.FindAsync(evento.Id);
                    if (eventoExistente != null)
                    {
                        eventoExistente.Evento = evento.Evento;
                        eventoExistente.Concluido = evento.Concluido;
                        eventoExistente.Ignorado = evento.Ignorado;
                        eventoExistente.DataEventoTicks = evento.DataEventoTicks;
                        
                        _auditoriaService.AtualizarAuditoria(eventoExistente, false);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Evento {evento.Id} atualizado");
                    }
                }

                // 5. Salvar todas as mudanças finais (assuntos, cronograma)
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💾 Salvando assuntos e cronograma no banco (transação única)");
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ EditalTransactionService.SalvarEditalComAssuntosAsync() FINALIZADO COM SUCESSO");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ EditalTransactionService - ERRO: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ StackTrace: {ex.StackTrace}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Salva apenas o edital sem operações relacionadas (para atualizações simples).
        /// </summary>
        public async Task SalvarEditalAsync(Edital edital, bool isNovoEdital)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔵 EditalTransactionService.SalvarEditalAsync() INICIADO");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] 📊 Edital ID={edital.Id}, Novo={isNovoEdital}");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (isNovoEdital)
                {
                    _auditoriaService.AtualizarAuditoria(edital, true);
                    _context.Editais.Add(edital);
                }
                else
                {
                    var editalExistente = await _context.Editais.FindAsync(edital.Id);
                    if (editalExistente == null)
                    {
                        throw new KeyNotFoundException("Edital não encontrado.");
                    }

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
                    editalExistente.BoletoPago = edital.BoletoPago;
                    editalExistente.DataAberturaTicks = edital.DataAberturaTicks;
                    editalExistente.DataProvaTicks = edital.DataProvaTicks;
                    editalExistente.Arquivado = edital.Arquivado;

                    _auditoriaService.AtualizarAuditoria(editalExistente, false);
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💾 Salvando edital no banco");
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ EditalTransactionService.SalvarEditalAsync() FINALIZADO COM SUCESSO");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ EditalTransactionService - ERRO: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ StackTrace: {ex.StackTrace}");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
