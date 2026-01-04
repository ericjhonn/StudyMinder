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
    /// Serviço para gerenciar transações de salvamento de disciplinas e assuntos atomicamente.
    /// Otimiza performance usando uma única transação para múltiplas operações.
    /// </summary>
    public class DisciplinaAssuntoTransactionService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;

        public DisciplinaAssuntoTransactionService(StudyMinderContext context, AuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }

        /// <summary>
        /// Salva uma disciplina e seus assuntos em uma única transação atômica.
        /// Apenas assuntos modificados ou novos são salvos.
        /// </summary>
        public async Task SalvarDisciplinaComAssuntosAsync(
            Disciplina disciplina,
            bool isNovaDisciplina,
            List<Assunto> assuntosModificados,
            List<Assunto> assuntosParaRemover,
            List<(Assunto assunto, int disciplinaDestinoId)> assuntosParaMover)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔵 DisciplinaAssuntoTransactionService.SalvarDisciplinaComAssuntosAsync() INICIADO");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] 📊 Disciplina ID={disciplina.Id}, Assuntos Modificados={assuntosModificados.Count}, Para Remover={assuntosParaRemover.Count}, Para Mover={assuntosParaMover.Count}");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Salvar disciplina
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 📝 Salvando disciplina");
                if (isNovaDisciplina)
                {
                    // Verificar nome único
                    if (await _context.Disciplinas.AnyAsync(d => d.Nome == disciplina.Nome))
                    {
                        throw new InvalidOperationException("Já existe uma disciplina com este nome.");
                    }

                    _auditoriaService.AtualizarAuditoria(disciplina, true);
                    _context.Disciplinas.Add(disciplina);
                }
                else
                {
                    // Verificar nome único (excluindo a disciplina atual)
                    if (await _context.Disciplinas.AnyAsync(d => d.Nome == disciplina.Nome && d.Id != disciplina.Id))
                    {
                        throw new InvalidOperationException("Já existe outra disciplina com este nome.");
                    }

                    var disciplinaExistente = await _context.Disciplinas.FirstOrDefaultAsync(d => d.Id == disciplina.Id);
                    if (disciplinaExistente == null)
                    {
                        throw new KeyNotFoundException("Disciplina não encontrada.");
                    }

                    disciplinaExistente.Nome = disciplina.Nome;
                    disciplinaExistente.Cor = disciplina.Cor;
                    disciplinaExistente.Arquivado = disciplina.Arquivado;
                    _auditoriaService.AtualizarAuditoria(disciplinaExistente, false);
                }

                // 2. Remover assuntos
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🗑️ Removendo {assuntosParaRemover.Count} assuntos");
                foreach (var assuntoRemovido in assuntosParaRemover)
                {
                    var assuntoExistente = await _context.Assuntos.FindAsync(assuntoRemovido.Id);
                    if (assuntoExistente != null)
                    {
                        _context.Assuntos.Remove(assuntoExistente);
                    }
                }

                // 3. Adicionar/Atualizar apenas assuntos modificados
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💾 Salvando {assuntosModificados.Count} assuntos modificados");
                foreach (var assunto in assuntosModificados)
                {
                    assunto.DisciplinaId = disciplina.Id;
                    assunto.AtualizarDataModificacao();

                    if (assunto.Id == 0)
                    {
                        // Novo assunto
                        // Verificar nome único na disciplina
                        if (await _context.Assuntos.AnyAsync(a =>
                            a.Nome == assunto.Nome &&
                            a.DisciplinaId == disciplina.Id))
                        {
                            throw new InvalidOperationException($"Já existe um assunto com o nome '{assunto.Nome}' nesta disciplina.");
                        }

                        _auditoriaService.AtualizarAuditoria(assunto, true);
                        _context.Assuntos.Add(assunto);
                    }
                    else
                    {
                        // Assunto editado
                        // Verificar nome único na disciplina (excluindo o assunto atual)
                        if (await _context.Assuntos.AnyAsync(a =>
                            a.Nome == assunto.Nome &&
                            a.DisciplinaId == disciplina.Id &&
                            a.Id != assunto.Id))
                        {
                            throw new InvalidOperationException($"Já existe outro assunto com o nome '{assunto.Nome}' nesta disciplina.");
                        }

                        var assuntoExistente = await _context.Assuntos.FindAsync(assunto.Id);
                        if (assuntoExistente == null)
                        {
                            throw new KeyNotFoundException($"Assunto com ID {assunto.Id} não encontrado.");
                        }

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
                    }
                }

                // 4. Mover assuntos para outra disciplina
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🚚 Movendo {assuntosParaMover.Count} assuntos");
                foreach (var (assunto, disciplinaDestinoId) in assuntosParaMover)
                {
                    var assuntoExistente = await _context.Assuntos.FindAsync(assunto.Id);
                    if (assuntoExistente != null)
                    {
                        assuntoExistente.DisciplinaId = disciplinaDestinoId;
                        _auditoriaService.AtualizarAuditoria(assuntoExistente, false);
                    }
                }

                // 5. Salvar todas as mudanças em uma única transação
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💾 Salvando todas as mudanças no banco (transação única)");
                await _context.SaveChangesAsync();

                // 6. Atualizar progresso e rendimento da disciplina (fora da transação de dados)
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔄 Atualizando progresso e rendimento da disciplina");
                var disciplinaParaAtualizar = await _context.Disciplinas
                    .Include(d => d.Assuntos)
                        .ThenInclude(a => a.Estudos)
                    .FirstOrDefaultAsync(d => d.Id == disciplina.Id);

                if (disciplinaParaAtualizar != null)
                {
                    disciplinaParaAtualizar.InvalidateProgressCache();
                    var progresso = disciplinaParaAtualizar.Progresso;
                    disciplinaParaAtualizar.AtualizarDataModificacao();
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ DisciplinaAssuntoTransactionService.SalvarDisciplinaComAssuntosAsync() FINALIZADO COM SUCESSO");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ DisciplinaAssuntoTransactionService - ERRO: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ StackTrace: {ex.StackTrace}");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
