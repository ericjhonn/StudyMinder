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
    /// Serviço para gerenciar transações de salvamento de estudos atomicamente.
    /// Otimiza performance usando uma única transação para múltiplas operações.
    /// </summary>
    public class EstudoTransactionService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;
        private readonly RevisaoNotificacaoService _revisaoNotificacaoService;

        public EstudoTransactionService(StudyMinderContext context, AuditoriaService auditoriaService, RevisaoNotificacaoService revisaoNotificacaoService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
            _revisaoNotificacaoService = revisaoNotificacaoService;
        }

        /// <summary>
        /// Salva um estudo com todas as operações relacionadas em uma única transação atômica.
        /// Inclui: salvar estudo, atualizar assunto, criar revisões agendadas, marcar revisão como concluída.
        /// </summary>
        public async Task SalvarEstudoComRevisoeseAssuntoAsync(
            Estudo estudo,
            bool isNovoEstudo,
            Assunto? assuntoParaAtualizar,
            bool? novoEstadoConcluido,
            List<Revisao> revisoesParaCriar,
            int? revisaoIdParaMarcarConcluida)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔵 EstudoTransactionService.SalvarEstudoComRevisoeseAssuntoAsync() INICIADO");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] 📊 Estudo ID={estudo.Id}, Novo={isNovoEstudo}, Revisões={revisoesParaCriar.Count}, Marcar Revisão={revisaoIdParaMarcarConcluida}");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Salvar estudo
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 📝 Salvando estudo");
                if (isNovoEstudo)
                {
                    _auditoriaService.AtualizarAuditoria(estudo, true);
                    _context.Estudos.Add(estudo);
                }
                else
                {
                    var estudoExistente = await _context.Estudos.FindAsync(estudo.Id);
                    if (estudoExistente == null)
                    {
                        throw new KeyNotFoundException("Estudo não encontrado.");
                    }

                    estudoExistente.TipoEstudoId = estudo.TipoEstudoId;
                    estudoExistente.AssuntoId = estudo.AssuntoId;
                    estudoExistente.DataTicks = estudo.DataTicks;
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
                }

                // 1.5. Salvar estudo PRIMEIRO para obter o ID (necessário para as revisões)
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💾 Salvando estudo para obter ID");
                await _context.SaveChangesAsync();

                // 2. Atualizar assunto se necessário
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 📚 Atualizando assunto (se necessário)");
                if (assuntoParaAtualizar != null && novoEstadoConcluido.HasValue)
                {
                    var assuntoExistente = await _context.Assuntos.FindAsync(assuntoParaAtualizar.Id);
                    if (assuntoExistente == null)
                    {
                        throw new KeyNotFoundException($"Assunto com ID {assuntoParaAtualizar.Id} não encontrado.");
                    }

                    // Impedir marcação como concluído se arquivado
                    if (novoEstadoConcluido.Value && assuntoExistente.Arquivado)
                    {
                        throw new InvalidOperationException("Não é possível marcar um assunto arquivado como concluído.");
                    }

                    // Só atualizar se houve mudança
                    if (assuntoExistente.Concluido != novoEstadoConcluido.Value)
                    {
                        assuntoExistente.Concluido = novoEstadoConcluido.Value;

                        if (assuntoExistente.Concluido)
                        {
                            assuntoExistente.MarcarComoConcluido();
                        }
                        else
                        {
                            assuntoExistente.MarcarComoNaoConcluido();
                        }

                        _auditoriaService.AtualizarAuditoria(assuntoExistente, false);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Assunto '{assuntoExistente.Nome}' atualizado - Concluído: {novoEstadoConcluido.Value}");
                    }
                }

                // 3. Criar revisões agendadas (agora com ID do estudo correto)
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 📅 Criando {revisoesParaCriar.Count} revisões agendadas");
                foreach (var revisao in revisoesParaCriar)
                {
                    revisao.EstudoOrigemId = estudo.Id;
                    _auditoriaService.AtualizarAuditoria(revisao, true);
                    _context.Revisoes.Add(revisao);
                }

                // 4. Marcar revisão como concluída (se em modo revisão)
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✔️ Marcando revisão como concluída (se aplicável)");
                if (revisaoIdParaMarcarConcluida.HasValue)
                {
                    var revisaoExistente = await _context.Revisoes.FindAsync(revisaoIdParaMarcarConcluida.Value);
                    if (revisaoExistente != null)
                    {
                        revisaoExistente.EstudoRealizadoId = estudo.Id;
                        _auditoriaService.AtualizarAuditoria(revisaoExistente, false);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Revisão {revisaoIdParaMarcarConcluida.Value} marcada como concluída");
                    }
                }

                // 4.5. Disparar notificação de revisão atualizada
                // Isso permite que HomeViewModel e outras views sejam notificadas da mudança em tempo real
                if (revisaoIdParaMarcarConcluida.HasValue)
                {
                    var revisaoAtualizada = await _context.Revisoes.FindAsync(revisaoIdParaMarcarConcluida.Value);
                    if (revisaoAtualizada != null)
                    {
                        _revisaoNotificacaoService.NotificarRevisaoAtualizada(revisaoAtualizada);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] 📢 Notificação disparada para revisão {revisaoIdParaMarcarConcluida.Value}");
                    }
                }

                // 5. Salvar todas as mudanças finais (revisões e atualizações)
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💾 Salvando revisões e mudanças finais no banco");
                await _context.SaveChangesAsync();

                // 6. Atualizar data de modificação do assunto (fora da transação de dados)
                if (assuntoParaAtualizar != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔄 Atualizando data de modificação do assunto");
                    var assuntoParaAtualizar_Db = await _context.Assuntos
                        .FirstOrDefaultAsync(a => a.Id == assuntoParaAtualizar.Id);

                    if (assuntoParaAtualizar_Db != null)
                    {
                        assuntoParaAtualizar_Db.AtualizarDataModificacao();
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ EstudoTransactionService.SalvarEstudoComRevisoeseAssuntoAsync() FINALIZADO COM SUCESSO");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ EstudoTransactionService - ERRO: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ StackTrace: {ex.StackTrace}");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
