using Microsoft.EntityFrameworkCore;
using StudyMinder.Data;
using StudyMinder.Models;
using System.Diagnostics;

namespace StudyMinder.Services
{
    public class RevisaoCicloAtivoService
    {
        private readonly StudyMinderContext _context;
        private readonly AuditoriaService _auditoriaService;
        
        // OTIMIZAÇÃO: Cache com timestamp para invalidação
        private List<Assunto>? _cacheAssuntosAtivos;
        private DateTime _cacheAssuntosAtivosTimestamp = DateTime.MinValue;
        private const int CACHE_DURATION_SECONDS = 30; // Cache válido por 30 segundos

        public RevisaoCicloAtivoService(StudyMinderContext context, AuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }
        
        /// <summary>
        /// Invalida o cache de assuntos ativos
        /// </summary>
        private void InvalidarCache()
        {
            _cacheAssuntosAtivos = null;
            _cacheAssuntosAtivosTimestamp = DateTime.MinValue;
        }

        public async Task<(bool Success, string ErrorMessage)> AdicionarAssuntoAoCicloAsync(int assuntoId)
        {
            try
            {
                Debug.WriteLine($"[RevisaoCicloAtivoService] Iniciando AdicionarAssuntoAoCicloAsync para AssuntoId: {assuntoId}");
                
                // Verificar se já existe
                var existente = await _context.RevisoesCicloAtivo
                    .FirstOrDefaultAsync(rca => rca.AssuntoId == assuntoId);

                if (existente != null)
                {
                    Debug.WriteLine($"[RevisaoCicloAtivoService] ⚠️ AssuntoId {assuntoId} já existe no ciclo");
                    return (false, "Este assunto já está no ciclo de revisão."); // Já existe no ciclo
                }

                var revisaoCicloAtivo = new RevisaoCicloAtivo
                {
                    AssuntoId = assuntoId,
                    DataInclusao = DateTime.Now
                };

                _context.RevisoesCicloAtivo.Add(revisaoCicloAtivo);
                Debug.WriteLine($"[RevisaoCicloAtivoService] Entidade adicionada ao contexto para AssuntoId: {assuntoId}");
                
                await _context.SaveChangesAsync();
                Debug.WriteLine($"[RevisaoCicloAtivoService] ✅ AssuntoId {assuntoId} persistido com sucesso no banco de dados");
                InvalidarCache(); // Invalidar cache após adicionar
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RevisaoCicloAtivoService] ❌ ERRO ao adicionar AssuntoId {assuntoId}: {ex.GetType().Name}");
                Debug.WriteLine($"[RevisaoCicloAtivoService] Mensagem: {ex.Message}");
                Debug.WriteLine($"[RevisaoCicloAtivoService] Stack Trace: {ex.StackTrace}");
                
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[RevisaoCicloAtivoService] InnerException: {ex.InnerException.Message}");
                    errorMessage = ex.InnerException.Message;
                }
                
                return (false, errorMessage);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> RemoverAssuntoDoCicloAsync(int assuntoId)
        {
            try
            {
                Debug.WriteLine($"[RevisaoCicloAtivoService] Iniciando RemoverAssuntoDoCicloAsync para AssuntoId: {assuntoId}");
                
                var existente = await _context.RevisoesCicloAtivo
                    .FirstOrDefaultAsync(rca => rca.AssuntoId == assuntoId);

                if (existente == null)
                {
                    Debug.WriteLine($"[RevisaoCicloAtivoService] ⚠️ AssuntoId {assuntoId} não encontrado no ciclo");
                    return (false, "Este assunto não está no ciclo de revisão.");
                }

                _context.RevisoesCicloAtivo.Remove(existente);
                Debug.WriteLine($"[RevisaoCicloAtivoService] Entidade marcada para remoção para AssuntoId: {assuntoId}");
                
                await _context.SaveChangesAsync();
                Debug.WriteLine($"[RevisaoCicloAtivoService] ✅ AssuntoId {assuntoId} removido com sucesso do banco de dados");
                InvalidarCache(); // Invalidar cache após remover
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RevisaoCicloAtivoService] ❌ ERRO ao remover AssuntoId {assuntoId}: {ex.GetType().Name}");
                Debug.WriteLine($"[RevisaoCicloAtivoService] Mensagem: {ex.Message}");
                Debug.WriteLine($"[RevisaoCicloAtivoService] Stack Trace: {ex.StackTrace}");
                
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[RevisaoCicloAtivoService] InnerException: {ex.InnerException.Message}");
                    errorMessage = ex.InnerException.Message;
                }
                
                return (false, errorMessage);
            }
        }

        public async Task<List<Assunto>> ObterAssuntosAtivosAsync()
        {
            // OTIMIZAÇÃO: Carregar todos os últimos estudos em UMA query (evita N+1)
            var ultimosEstudosPorAssunto = await _context.Estudos
                .GroupBy(e => e.AssuntoId)
                .Select(g => new { 
                    AssuntoId = g.Key, 
                    UltimoDataTicks = g.OrderByDescending(e => e.DataTicks).Select(e => e.DataTicks).First()
                })
                .ToDictionaryAsync(x => x.AssuntoId, x => x.UltimoDataTicks);

            // Carregar assuntos ativos com disciplinas
            var assuntosAtivos = await _context.RevisoesCicloAtivo
                .Include(rca => rca.Assunto)
                .ThenInclude(a => a.Disciplina)
                .Where(rca => !rca.Assunto.Arquivado)
                .ToListAsync();

            // Processar em memória (dados já carregados)
            var assuntosComData = assuntosAtivos
                .Select(rca => new
                {
                    Assunto = rca.Assunto,
                    UltimoEstudoDataTicks = ultimosEstudosPorAssunto.GetValueOrDefault(rca.Assunto.Id, 0)
                })
                .OrderBy(x => x.UltimoEstudoDataTicks == 0 ? 0 : 1)  // Nunca estudados primeiro
                .ThenBy(x => x.UltimoEstudoDataTicks)                // Depois por data (antigos primeiro)
                .ThenBy(x => x.Assunto.Nome)                         // Desempate: alfabético
                .ToList();

            // Adicionar a data do último estudo como propriedade calculada
            var assuntos = new List<Assunto>();
            foreach (var item in assuntosComData)
            {
                item.Assunto.DataUltimoEstudo = item.UltimoEstudoDataTicks == 0 ? null : new DateTime(item.UltimoEstudoDataTicks);
                assuntos.Add(item.Assunto);
            }

            return assuntos;
        }

        public async Task<List<Assunto>> ObterAssuntosDisponiveisAsync()
        {
            // Obter IDs dos assuntos que já estão no ciclo
            var assuntosAtivosIds = await _context.RevisoesCicloAtivo
                .Select(rca => rca.AssuntoId)
                .ToListAsync();

            // Retornar assuntos que NÃO estão no ciclo e não estão arquivados
            return await _context.Assuntos
                .Include(a => a.Disciplina)
                .Where(a => !assuntosAtivosIds.Contains(a.Id) && !a.Arquivado)
                .OrderBy(a => a.Nome)
                .ToListAsync();
        }

        public async Task<bool> DefinirAssuntosAtivosAsync(List<int> assuntoIds)
        {
            try
            {
                Debug.WriteLine($"[RevisaoCicloAtivoService] Iniciando DefinirAssuntosAtivosAsync com {assuntoIds.Count} assuntos");
                
                // Remover todos os assuntos ativos atuais
                var atuais = await _context.RevisoesCicloAtivo.ToListAsync();
                Debug.WriteLine($"[RevisaoCicloAtivoService] Removendo {atuais.Count} assuntos ativos anteriores");
                _context.RevisoesCicloAtivo.RemoveRange(atuais);

                // Adicionar novos assuntos ativos
                var novos = assuntoIds.Select(id => new RevisaoCicloAtivo
                {
                    AssuntoId = id,
                    DataInclusao = DateTime.Now
                }).ToList();

                Debug.WriteLine($"[RevisaoCicloAtivoService] Adicionando {novos.Count} novos assuntos ativos");
                await _context.RevisoesCicloAtivo.AddRangeAsync(novos);
                await _context.SaveChangesAsync();
                Debug.WriteLine($"[RevisaoCicloAtivoService] ✅ DefinirAssuntosAtivosAsync concluído com sucesso");
                InvalidarCache(); // Invalidar cache após modificar
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RevisaoCicloAtivoService] ❌ ERRO em DefinirAssuntosAtivosAsync: {ex.GetType().Name}");
                Debug.WriteLine($"[RevisaoCicloAtivoService] Mensagem: {ex.Message}");
                Debug.WriteLine($"[RevisaoCicloAtivoService] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[RevisaoCicloAtivoService] InnerException: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<int> ObterTotalAssuntosAtivosAsync()
        {
            return await _context.RevisoesCicloAtivo
                .Include(rca => rca.Assunto)
                .CountAsync(rca => !rca.Assunto.Arquivado);
        }

        public async Task<bool> EstaAssuntoAtivoAsync(int assuntoId)
        {
            return await _context.RevisoesCicloAtivo
                .AnyAsync(rca => rca.AssuntoId == assuntoId);
        }

        public async Task<PagedResult<Assunto>> ObterAssuntosPaginadoAsync(int paginaAtual, int itensPorPagina)
        {
            // Contar total de assuntos ativos
            var totalCount = await _context.RevisoesCicloAtivo
                .Include(rca => rca.Assunto)
                .CountAsync(rca => !rca.Assunto.Arquivado);

            // Obter assuntos da página solicitada com dados do último estudo
            var assuntosComData = await _context.RevisoesCicloAtivo
                .Include(rca => rca.Assunto)
                .ThenInclude(a => a.Disciplina)
                .Where(rca => !rca.Assunto.Arquivado)
                .Select(rca => new
                {
                    Assunto = rca.Assunto,
                    UltimoEstudoDataTicks = _context.Estudos
                        .Where(e => e.AssuntoId == rca.Assunto.Id)
                        .OrderByDescending(e => e.DataTicks)
                        .Select(e => e.DataTicks)
                        .FirstOrDefault()
                })
                .OrderBy(x => x.UltimoEstudoDataTicks == 0 ? 0 : 1)  // Nunca estudados primeiro
                .ThenBy(x => x.UltimoEstudoDataTicks)                // Depois por data (antigos primeiro)
                .ThenBy(x => x.Assunto.Nome)                         // Desempate: alfabético
                .Skip((paginaAtual - 1) * itensPorPagina)
                .Take(itensPorPagina)
                .ToListAsync();

            // Adicionar a data do último estudo como propriedade calculada
            var assuntos = new List<Assunto>();
            foreach (var item in assuntosComData)
            {
                item.Assunto.DataUltimoEstudo = item.UltimoEstudoDataTicks == 0 ? null : new DateTime(item.UltimoEstudoDataTicks);
                assuntos.Add(item.Assunto);
            }

            return new PagedResult<Assunto>
            {
                Items = assuntos,
                TotalCount = totalCount,
                TotalItems = totalCount,
                PageNumber = paginaAtual,
                PageSize = itensPorPagina
            };
        }

        public async Task<PagedResult<Assunto>> ObterAssuntosDisponiveisPaginadoAsync(int paginaAtual, int itensPorPagina)
        {
            // OTIMIZAÇÃO: Usar subquery em vez de carregar IDs em memória
            // Isso evita 2 queries separadas e usa apenas 1 query otimizada
            
            var query = _context.Assuntos
                .Where(a => !a.Arquivado && !_context.RevisoesCicloAtivo.Any(rca => rca.AssuntoId == a.Id))
                .Include(a => a.Disciplina);

            // Contar total (sem paginação)
            var totalCount = await query.CountAsync();

            // Obter página com paginação no banco
            var assuntos = await query
                .OrderBy(a => a.Nome)
                .Skip((paginaAtual - 1) * itensPorPagina)
                .Take(itensPorPagina)
                .ToListAsync();

            return new PagedResult<Assunto>
            {
                Items = assuntos,
                TotalCount = totalCount,
                TotalItems = totalCount,
                PageNumber = paginaAtual,
                PageSize = itensPorPagina
            };
        }
    }
}
