using Microsoft.EntityFrameworkCore;
using StudyMinder.Data;
using StudyMinder.Models;
using System.Globalization;
using System.Text;

namespace StudyMinder.Services
{
    public class RevisaoService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;

        public RevisaoService(StudyMinderContext context, AuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }

        /// <summary>
        /// Remove acentos e caracteres especiais de uma string para comparação
        /// </summary>
        private static string RemoverAcentos(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return texto;

            var textoNormalizado = texto.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in textoNormalizado)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public async Task<PagedResult<Revisao>> ObterRevisoesPendentesPorTipoAsync(
            TipoRevisaoEnum tipoRevisao, 
            int pagina = 1, 
            int itensPorPagina = 20)
        {
            var hoje = DateTime.Today;
            
            var query = _context.Revisoes
                .Include(r => r.EstudoOrigem)
                .ThenInclude(e => e.Assunto)
                .ThenInclude(a => a.Disciplina)
                .Where(r => r.TipoRevisao == tipoRevisao && 
                           r.EstudoRealizadoId == null && 
                           r.DataProgramada.Date <= hoje)
                .OrderBy(r => r.DataProgramada);

            var totalItems = await query.CountAsync();
                
                var items = await query
                    .Skip((pagina - 1) * itensPorPagina)
                    .Take(itensPorPagina)
                    .Include(r => r.EstudoOrigem)
                        .ThenInclude(e => e.Assunto)
                            .ThenInclude(a => a.Disciplina)
                    .ToListAsync();

                return new PagedResult<Revisao>
                {
                    Items = items,
                    TotalItems = totalItems,
                    PageNumber = pagina,
                    PageSize = itensPorPagina
                };
        }

        public async Task<List<Assunto>> ObterAssuntosParaCicloAsync()
        {
            // Obter IDs dos assuntos ativos no ciclo
            var assuntosAtivosIds = await _context.RevisoesCicloAtivo
                .Select(rca => rca.AssuntoId)
                .ToListAsync();

            if (!assuntosAtivosIds.Any())
                return new List<Assunto>();

            // Obter assuntos com data do último estudo
            var assuntosComData = await _context.Assuntos
                .Include(a => a.Disciplina)
                .Where(a => assuntosAtivosIds.Contains(a.Id) && !a.Arquivado)
                .Select(a => new
                {
                    Assunto = a,
                    UltimoEstudoDataTicks = _context.Estudos
                        .Where(e => e.AssuntoId == a.Id)
                        .OrderByDescending(e => e.DataTicks)
                        .Select(e => e.DataTicks)
                        .FirstOrDefault()
                })
                .OrderBy(x => x.UltimoEstudoDataTicks == 0 ? 0 : x.UltimoEstudoDataTicks) // Não estudados primeiro
                .ThenBy(x => x.Assunto.Nome)
                .ToListAsync();

            // Adicionar a data do último estudo como propriedade calculada
            var assuntos = new List<Assunto>();
            foreach (var item in assuntosComData)
            {
                item.Assunto.DataUltimoEstudo = item.UltimoEstudoDataTicks == 0 ? null : new DateTime(item.UltimoEstudoDataTicks);
                assuntos.Add(item.Assunto);
            }

            return assuntos;
        }

        public async Task<Assunto?> ObterPrimeiroAssuntoCicloAsync()
        {
            // Obter IDs dos assuntos ativos no ciclo
            var assuntosAtivosIds = await _context.RevisoesCicloAtivo
                .Select(rca => rca.AssuntoId)
                .ToListAsync();

            if (!assuntosAtivosIds.Any())
                return null;

            // Obter apenas o primeiro assunto com data do último estudo
            var primeiroComData = await _context.Assuntos
                .Include(a => a.Disciplina)
                .Where(a => assuntosAtivosIds.Contains(a.Id) && !a.Arquivado)
                .Select(a => new
                {
                    Assunto = a,
                    UltimoEstudoDataTicks = _context.Estudos
                        .Where(e => e.AssuntoId == a.Id)
                        .OrderByDescending(e => e.DataTicks)
                        .Select(e => e.DataTicks)
                        .FirstOrDefault()
                })
                .OrderBy(x => x.UltimoEstudoDataTicks == 0 ? 0 : x.UltimoEstudoDataTicks) // Não estudados primeiro
                .ThenBy(x => x.Assunto.Nome)
                .FirstOrDefaultAsync();

            if (primeiroComData == null)
                return null;

            // Adicionar a data do último estudo como propriedade calculada
            primeiroComData.Assunto.DataUltimoEstudo = primeiroComData.UltimoEstudoDataTicks == 0 
                ? null 
                : new DateTime(primeiroComData.UltimoEstudoDataTicks);

            return primeiroComData.Assunto;
        }

        public async Task<bool> MarcarComoConcluidaAsync(int revisaoId, int estudoRealizadoId)
        {
            try
            {
                var revisao = await _context.Revisoes
                    .FirstOrDefaultAsync(r => r.Id == revisaoId);

                if (revisao == null)
                    return false;

                revisao.EstudoRealizadoId = estudoRealizadoId;
                revisao.AtualizarDataModificacao();

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CriarRevisaoCiclicaConcluidaAsync(int estudoOrigemId, int estudoRealizadoId)
        {
            try
            {
                var estudoOrigem = await _context.Estudos
                    .Include(e => e.Assunto)
                    .FirstOrDefaultAsync(e => e.Id == estudoOrigemId);

                if (estudoOrigem == null)
                    return false;

                var revisao = new Revisao
                {
                    EstudoOrigemId = estudoOrigem.Id,
                    TipoRevisao = TipoRevisaoEnum.Ciclico,
                    DataProgramada = DateTime.Now, // Já está concluída
                    DataCriacao = DateTime.Now,
                    DataModificacaoTicks = DateTime.Now.Ticks
                };

                _context.Revisoes.Add(revisao);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<TipoRevisaoEnum, int>> ObterEstatisticasAsync()
        {
            var hoje = DateTime.Today;
            
            var estatisticas = await _context.Revisoes
                .GroupBy(r => r.TipoRevisao)
                .Select(g => new
                {
                    Tipo = g.Key,
                    Pendentes = g.Count(r => r.EstudoRealizadoId == null && r.DataProgramada.Date <= hoje),
                    Concluidas = g.Count(r => r.EstudoRealizadoId != null)
                })
                .ToListAsync();

            var resultado = new Dictionary<TipoRevisaoEnum, int>();
            
            foreach (var tipo in Enum.GetValues<TipoRevisaoEnum>())
            {
                var estatistica = estatisticas.FirstOrDefault(e => e.Tipo == tipo);
                resultado[tipo] = estatistica?.Pendentes ?? 0;
            }

            return resultado;
        }

        public async Task<List<Revisao>> ObterRevisoesRecentesAsync(int limite = 10)
        {
            return await _context.Revisoes
                .Include(r => r.EstudoOrigem)
                .ThenInclude(e => e.Assunto)
                .ThenInclude(a => a.Disciplina)
                .Include(r => r.EstudoRealizado)
                .OrderByDescending(r => r.DataModificacaoTicks) // Ordenar por data de modificação (mais recentes primeiro)
                .Take(limite)
                .ToListAsync();
        }

        /// <summary>
        /// Obtém revisões pendentes filtradas por tipos específicos
        /// </summary>
        public async Task<PagedResult<Revisao>> ObterRevisoesPendentesAsync(
            List<TipoRevisaoEnum> tiposRevisao,
            int pagina = 1,
            int itensPorPagina = 20,
            string? termoPesquisa = null)
        {
            try
            {
                var hoje = DateTime.Today;
                var hojeTicks = hoje.Ticks;
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] ObterRevisoesPendentesAsync iniciado");
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Tipos de revisão: {string.Join(", ", tiposRevisao)}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Hoje: {hoje}, Ticks: {hojeTicks}");
                
                // Verificar quantas revisões existem no total
                var totalRevisoes = await _context.Revisoes.CountAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Total de revisões na tabela: {totalRevisoes}");
                
                // Verificar revisões pendentes (sem EstudoRealizadoId)
                var revisoesPendentes = await _context.Revisoes
                    .Where(r => r.EstudoRealizadoId == null)
                    .CountAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Revisões pendentes (EstudoRealizadoId == null): {revisoesPendentes}");
                
                // Verificar revisões com tipos corretos
                var revisoesTiposCorretos = await _context.Revisoes
                    .Where(r => tiposRevisao.Contains(r.TipoRevisao))
                    .CountAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Revisões com tipos corretos: {revisoesTiposCorretos}");
                
                // Verificar revisões com data programada (todas, incluindo futuras)
                var revisoesDataCorreta = await _context.Revisoes
                    .Where(r => r.DataProgramadaTicks <= hojeTicks)
                    .CountAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Revisões com data programada <= hoje: {revisoesDataCorreta}");

                var query = _context.Revisoes
                    .AsNoTracking()
                    .Include(r => r.EstudoOrigem)
                        .ThenInclude(e => e.Assunto)
                            .ThenInclude(a => a.Disciplina)
                    .Where(r => tiposRevisao.Contains(r.TipoRevisao) &&
                               r.EstudoRealizadoId == null)
                    .OrderBy(r => r.DataProgramadaTicks);

                // Trazer todos os dados do banco primeiro
                var todasAsRevisoes = await query.ToListAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Total de revisões antes do filtro de pesquisa: {todasAsRevisoes.Count}");

                // Aplicar filtro de pesquisa em memória (case-insensitive e sem acentos)
                if (!string.IsNullOrWhiteSpace(termoPesquisa))
                {
                    var termoPesquisaNormalizado = RemoverAcentos(termoPesquisa).ToUpper();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Termo de pesquisa normalizado: '{termoPesquisaNormalizado}'");
                    
                    todasAsRevisoes = todasAsRevisoes.Where(r => 
                        RemoverAcentos(r.EstudoOrigem?.Assunto?.Nome ?? "").ToUpper().Contains(termoPesquisaNormalizado) ||
                        RemoverAcentos(r.EstudoOrigem?.Assunto?.Disciplina?.Nome ?? "").ToUpper().Contains(termoPesquisaNormalizado))
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Total de revisões após filtro de pesquisa: {todasAsRevisoes.Count}");
                }

                var totalCount = todasAsRevisoes.Count;
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Total de revisões para paginação: {totalCount}");
                
                var items = todasAsRevisoes
                    .Skip((pagina - 1) * itensPorPagina)
                    .Take(itensPorPagina)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Itens carregados: {items.Count}");
                foreach (var item in items)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Revisão ID={item.Id}, EstudoOrigemId={item.EstudoOrigemId}, EstudoOrigem={item.EstudoOrigem?.Id}");
                }

                return new PagedResult<Revisao>
                {
                    Items = items,
                    TotalItems = totalCount,
                    TotalCount = totalCount,
                    PageNumber = pagina,
                    PageSize = itensPorPagina
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] ❌ ERRO em ObterRevisoesPendentesAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG-SERVICE] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Marca uma revisão como concluída e agenda próxima revisão se necessário
        /// </summary>
        public async Task<bool> ConcluirRevisaoAsync(int revisaoId, int estudoRealizadoId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var revisao = await _context.Revisoes
                    .Include(r => r.EstudoOrigem)
                    .FirstOrDefaultAsync(r => r.Id == revisaoId);

                if (revisao == null)
                    return false;

                // Marcar revisão como concluída
                revisao.EstudoRealizadoId = estudoRealizadoId;
                revisao.AtualizarDataModificacao();

                // Verificar se precisa agendar próxima revisão (apenas para métodos clássicos)
                await AgendarProximaRevisaoSeNecessarioAsync(revisao);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        /// <summary>
        /// Agenda próxima revisão para métodos clássicos em sequência
        /// </summary>
        private async Task AgendarProximaRevisaoSeNecessarioAsync(Revisao revisaoAtual)
        {
            var proximoTipo = ObterProximoTipoRevisao(revisaoAtual.TipoRevisao);
            
            if (proximoTipo.HasValue)
            {
                var dataProximaRevisao = CalcularDataProximaRevisao(proximoTipo.Value);
                
                var novaRevisao = new Revisao
                {
                    EstudoOrigemId = revisaoAtual.EstudoOrigemId,
                    EstudoRealizadoId = null,
                    TipoRevisao = proximoTipo.Value,
                    DataProgramada = dataProximaRevisao,
                    DataCriacao = DateTime.UtcNow,
                    DataModificacaoTicks = DateTime.UtcNow.Ticks
                };

                _context.Revisoes.Add(novaRevisao);
                await Task.CompletedTask; // Para satisfazer o async
            }
        }

        /// <summary>
        /// Exclui uma revisão individual do banco de dados
        /// </summary>
        public async Task<bool> ExcluirRevisaoAsync(int revisaoId)
        {
            try
            {
                var revisao = await _context.Revisoes.FindAsync(revisaoId);
                if (revisao == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Revisão {revisaoId} não encontrada para exclusão");
                    return false;
                }

                // Verificar se a revisão já foi concluída
                if (revisao.EstudoRealizadoId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Revisão {revisaoId} já foi concluída, não pode ser excluída");
                    return false;
                }

                _context.Revisoes.Remove(revisao);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Revisão {revisaoId} excluída com sucesso");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Erro ao excluir revisão {revisaoId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Salva uma revisão cíclica completamente preenchida (Origem e Realizado)
        /// </summary>
        public async Task<bool> SalvarRevisaoCiclicaAsync(int estudoOrigemId, int estudoRealizadoId)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                var revisaoCiclica = new Revisao
                {
                    EstudoOrigemId = estudoOrigemId,
                    EstudoRealizadoId = estudoRealizadoId,
                    TipoRevisao = TipoRevisaoEnum.Ciclico,
                    DataProgramada = DateTime.Today, // Data de hoje
                    DataCriacao = DateTime.UtcNow,
                    DataModificacao = DateTime.UtcNow
                };

                _context.Revisoes.Add(revisaoCiclica);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Revisão cíclica salva: Origem={estudoOrigemId}, Realizado={estudoRealizadoId}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Erro ao salvar revisão cíclica: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cria uma única revisão no banco de dados
        /// </summary>
        public async Task<bool> CriarRevisaoAsync(int estudoOrigemId, TipoRevisaoEnum tipo, DateTime dataProgramada)
        {
            try
            {
                var revisao = new Revisao
                {
                    EstudoOrigemId = estudoOrigemId,
                    TipoRevisao = tipo,
                    DataProgramada = dataProgramada,
                    DataCriacao = DateTime.UtcNow,
                    DataModificacao = DateTime.UtcNow
                };

                _context.Revisoes.Add(revisao);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Revisão criada: ID={revisao.Id}, Tipo={tipo}, Data={dataProgramada:dd/MM/yyyy}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Erro ao criar revisão: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determina o próximo tipo de revisão na sequência clássica
        /// Sequência: 24h → 7d → 30d → 90d → 120d → 180d
        /// </summary>
        private TipoRevisaoEnum? ObterProximoTipoRevisao(TipoRevisaoEnum tipoAtual)
        {
            return tipoAtual switch
            {
                TipoRevisaoEnum.Classico24h => TipoRevisaoEnum.Classico7d,
                TipoRevisaoEnum.Classico7d => TipoRevisaoEnum.Classico30d,
                TipoRevisaoEnum.Classico30d => TipoRevisaoEnum.Classico90d,
                TipoRevisaoEnum.Classico90d => TipoRevisaoEnum.Classico120d,
                TipoRevisaoEnum.Classico120d => TipoRevisaoEnum.Classico180d,
                TipoRevisaoEnum.Classico180d => null, // Fim da sequência
                TipoRevisaoEnum.Ciclo42 => null, // Não tem sequência automática
                TipoRevisaoEnum.Ciclico => null, // Não tem sequência automática
                _ => null
            };
        }

        /// <summary>
        /// Calcula a data da próxima revisão baseada no tipo
        /// Sequência clássica estendida com intervalos de longo prazo (Ebbinghaus aprimorado)
        /// </summary>
        private DateTime CalcularDataProximaRevisao(TipoRevisaoEnum tipo)
        {
            var agora = DateTime.Now;
            
            return tipo switch
            {
                TipoRevisaoEnum.Classico24h => agora.AddDays(1),      // 1 dia - Fixação imediata
                TipoRevisaoEnum.Classico7d => agora.AddDays(7),       // 7 dias - Memória curto prazo
                TipoRevisaoEnum.Classico30d => agora.AddDays(30),     // 30 dias - Consolidação média
                TipoRevisaoEnum.Classico90d => agora.AddDays(90),     // 90 dias - Retenção estendida
                TipoRevisaoEnum.Classico120d => agora.AddDays(120),   // 120 dias - Revisão de longo prazo
                TipoRevisaoEnum.Classico180d => agora.AddDays(180),   // 180 dias - Consolidação máxima
                TipoRevisaoEnum.Ciclo42 => agora.AddDays(1),          // Lógica específica do 4.2
                _ => agora.AddDays(1)
            };
        }
    }
}
