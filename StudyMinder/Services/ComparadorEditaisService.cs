using Microsoft.EntityFrameworkCore;
using StudyMinder.Data;
using StudyMinder.Models;
using StudyMinder.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudyMinder.Services
{
    public class ComparadorEditaisService
    {
        private readonly StudyMinderContext _context;
        
        // --- CACHE: Dictionary para armazenar Editais já carregados ---
        private readonly Dictionary<int, Edital> _cacheEditais = new();
        
        // --- CACHE: Registro de tempo para invalidação inteligente ---
        private readonly Dictionary<int, DateTime> _cacheTempo = new();
        private readonly TimeSpan _cacheValidade = TimeSpan.FromHours(1); // 1 hora

        public ComparadorEditaisService(StudyMinderContext context)
        {
            _context = context;
        }

        public async Task<ResultadoComparacao> CompararAsync(int idBase, int idAlvo)
        {
            // 1. Carregar Editais Completos (AsNoTracking para performance)
            var editalBase = await CarregarEditalCompleto(idBase);
            var editalAlvo = await CarregarEditalCompleto(idAlvo);

            var resultado = new ResultadoComparacao
            {
                EditalBase = editalBase,
                EditalAlvo = editalAlvo
            };

            // 2. Extrair Assuntos
            var assuntosBase = editalBase.EditalAssuntos.Select(ea => ea.Assunto).ToList();
            var assuntosAlvo = editalAlvo.EditalAssuntos.Select(ea => ea.Assunto).ToList();

            // 3. Criar HashSet para busca rápida O(1)
            var idsBase = new HashSet<int>(assuntosBase.Select(a => a.Id));
            var idsAlvo = new HashSet<int>(assuntosAlvo.Select(a => a.Id));

            // 4. Classificar assuntos do ALVO (Comuns vs Novos)
            foreach (var assunto in assuntosAlvo)
            {
                bool existeNoBase = idsBase.Contains(assunto.Id);

                if (existeNoBase)
                {
                    // Comum aos dois
                    if (assunto.Concluido)
                        resultado.AssuntosConciliaveisConcluidos.Add(assunto);
                    else
                        resultado.AssuntosConciliaveisPendentes.Add(assunto); // Lista de Ouro
                }
                else
                {
                    // Exclusivo do Alvo
                    if (assunto.Concluido)
                        resultado.AssuntosExclusivosAlvoConcluidos.Add(assunto);
                    else
                        resultado.AssuntosExclusivosAlvoPendentes.Add(assunto);
                }
            }

            // 5. Classificar assuntos do BASE (Apenas Exclusivos)
            foreach (var assunto in assuntosBase)
            {
                if (!idsAlvo.Contains(assunto.Id))
                {
                    resultado.AssuntosExclusivosBase.Add(assunto);
                }
            }

            // 6. Ordenação para UI (Agrupado por Disciplina)
            OrdenarListas(resultado);

            // 7. Cálculos Finais
            resultado.CalcularMetricas();
            resultado.CalcularPrioridadeTemporal();

            return resultado;
        }

        private async Task<Edital> CarregarEditalCompleto(int id)
        {
            // ETAPA 1: Verificar se está em cache e ainda é válido
            if (_cacheEditais.ContainsKey(id) && _cacheTempo.ContainsKey(id))
            {
                var tempoDecorrido = DateTime.UtcNow - _cacheTempo[id];
                if (tempoDecorrido < _cacheValidade)
                {
                    System.Diagnostics.Debug.WriteLine($"[CACHE HIT] Edital ID {id} carregado do cache (idade: {tempoDecorrido.TotalSeconds:F1}s)");
                    return _cacheEditais[id];
                }
            }

            // ETAPA 2: Não está em cache ou expirou - carregar do BD
            System.Diagnostics.Debug.WriteLine($"[CACHE MISS] Edital ID {id} carregando do banco de dados");
            
            var edital = await _context.Editais
                .AsNoTracking()
                .Include(e => e.EditalAssuntos)
                    .ThenInclude(ea => ea.Assunto)
                        .ThenInclude(a => a.Disciplina) // Necessário para agrupamento visual
                .FirstOrDefaultAsync(e => e.Id == id);

            // VALIDAÇÃO 1: Edital não encontrado
            if (edital == null)
            {
                throw new ArgumentException($"Edital com ID {id} não encontrado.");
            }

            // VALIDAÇÃO 2: Edital sem assuntos
            if (edital.EditalAssuntos == null || edital.EditalAssuntos.Count == 0)
            {
                throw new InvalidOperationException($"O edital '{edital.Nome}' não possui assuntos associados. Impossível realizar comparação.");
            }

            // ETAPA 3: Armazenar em cache
            _cacheEditais[id] = edital;
            _cacheTempo[id] = DateTime.UtcNow;
            System.Diagnostics.Debug.WriteLine($"[CACHE STORE] Edital ID {id} armazenado em cache");

            return edital;
        }

        private void OrdenarListas(ResultadoComparacao resultado)
        {
            resultado.AssuntosConciliaveisPendentes = OrdenarPorDisciplina(resultado.AssuntosConciliaveisPendentes);
            resultado.AssuntosConciliaveisConcluidos = OrdenarPorDisciplina(resultado.AssuntosConciliaveisConcluidos);
            resultado.AssuntosExclusivosAlvoPendentes = OrdenarPorDisciplina(resultado.AssuntosExclusivosAlvoPendentes);
            resultado.AssuntosExclusivosBase = OrdenarPorDisciplina(resultado.AssuntosExclusivosBase);
        }

        private List<Assunto> OrdenarPorDisciplina(List<Assunto> lista)
        {
            return lista
                .OrderBy(a => a.Disciplina.Nome)
                .ThenBy(a => a.Nome)
                .ToList();
        }

        // --- CACHE: Métodos públicos de gerenciamento ---

        /// <summary>
        /// Limpa o cache de editais. Útil quando dados são atualizados no banco de dados.
        /// </summary>
        public void ClearCache()
        {
            _cacheEditais.Clear();
            _cacheTempo.Clear();
            System.Diagnostics.Debug.WriteLine("[CACHE CLEAR] Cache de editais foi limpo");
        }

        /// <summary>
        /// Invalida um edital específico do cache.
        /// </summary>
        public void InvalidarCacheEdital(int idEdital)
        {
            if (_cacheEditais.ContainsKey(idEdital))
            {
                _cacheEditais.Remove(idEdital);
                _cacheTempo.Remove(idEdital);
                System.Diagnostics.Debug.WriteLine($"[CACHE INVALIDATE] Edital ID {idEdital} removido do cache");
            }
        }

        /// <summary>
        /// Retorna informações sobre o estado do cache (útil para debug).
        /// </summary>
        public (int ItemsCacheados, int ItemsValidos) GetCacheInfo()
        {
            var agora = DateTime.UtcNow;
            var itemsValidos = _cacheTempo.Count(kvp => (agora - kvp.Value) < _cacheValidade);
            return (_cacheEditais.Count, itemsValidos);
        }
    }
}