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
                .Select(g => new
                {
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

        public async Task<PagedResult<Assunto>> ObterAssuntosDisponiveisPaginadoAsync(int paginaAtual, int itensPorPagina)
        {
            // CRITÉRIO 1: LISTAR APENAS ASSUNTOS CONCLUÍDOS E NÃO ARQUIVADOS
            // NOTA: Ignoramos os parâmetros de paginação do banco para trazer a lista completa e permitir filtro na UI

            // 1. Obter IDs que já estão no ciclo (para excluir da lista)
            var idsNoCiclo = await _context.RevisoesCicloAtivo
                .Select(rca => rca.AssuntoId)
                .ToListAsync();

            // 2. Buscar TODOS os assuntos que atendem aos critérios
            var todosAssuntos = await _context.Assuntos
                .Include(a => a.Disciplina)
                .Where(a => !a.Arquivado
                       && a.Concluido == true  // <--- Correção: Filtra apenas concluídos no banco
                       && !idsNoCiclo.Contains(a.Id))
                .OrderBy(a => a.Nome)
                .ToListAsync();

            // 3. Retornar tudo envelopado em um PagedResult "falso" (página única contendo tudo)
            return new PagedResult<Assunto>
            {
                Items = todosAssuntos,
                TotalCount = todosAssuntos.Count,
                TotalItems = todosAssuntos.Count,
                PageNumber = 1,
                PageSize = todosAssuntos.Count > 0 ? todosAssuntos.Count : 100
            };
        }

        public async Task<PagedResult<Assunto>> ObterAssuntosPaginadoAsync(int paginaAtual, int itensPorPagina)
        {
            // CRITÉRIO 2: LISTAR TODOS ATIVOS, PRIORIZANDO OS MAIS ANTIGOS (OU NUNCA ESTUDADOS)

            // 1. OTIMIZAÇÃO: Carregar data do último estudo em UMA query agrupada
            var ultimosEstudosDict = await _context.Estudos
                .GroupBy(e => e.AssuntoId)
                .Select(g => new {
                    AssuntoId = g.Key,
                    UltimoDataTicks = g.Max(x => x.DataTicks)
                })
                .ToDictionaryAsync(k => k.AssuntoId, v => v.UltimoDataTicks);

            // 2. Carregar TODOS os assuntos ativos
            var assuntosDoCiclo = await _context.RevisoesCicloAtivo
                .Include(rca => rca.Assunto)
                .ThenInclude(a => a.Disciplina)
                .Select(rca => rca.Assunto)
                .ToListAsync();

            // 3. Preencher a data e Ordenar em Memória
            foreach (var assunto in assuntosDoCiclo)
            {
                if (ultimosEstudosDict.TryGetValue(assunto.Id, out long ticks))
                {
                    assunto.DataUltimoEstudo = new DateTime(ticks);
                }
                else
                {
                    assunto.DataUltimoEstudo = null; // Nunca estudado
                }
            }

            // Ordenação: Nulos primeiro (nunca estudado), depois datas antigas primeiro
            var listaOrdenada = assuntosDoCiclo
                .OrderBy(a => a.DataUltimoEstudo.HasValue) // false (null) vem antes
                .ThenBy(a => a.DataUltimoEstudo)
                .ToList();

            // 4. Retornar tudo envelopado
            return new PagedResult<Assunto>
            {
                Items = listaOrdenada,
                TotalCount = listaOrdenada.Count,
                TotalItems = listaOrdenada.Count,
                PageNumber = 1,
                PageSize = listaOrdenada.Count > 0 ? listaOrdenada.Count : 100
            };
        }
    }
}
