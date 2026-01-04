using System;
using System.Collections.Generic;
using StudyMinder.Models;

namespace StudyMinder.Models.DTOs
{
    public class ResultadoComparacao
    {
        public Edital EditalBase { get; set; } = null!;
        public Edital EditalAlvo { get; set; } = null!;

        // --- LISTAS ESTRATÉGICAS ---

        // A "Lista de Ouro": Comuns e Pendentes (Prioridade Máxima)
        public List<Assunto> AssuntosConciliaveisPendentes { get; set; } = new();

        // Comuns e já Concluídos (Sua bagagem garantida)
        public List<Assunto> AssuntosConciliaveisConcluidos { get; set; } = new();

        // Exclusivos do Alvo e Pendentes (O "Gap" a estudar)
        public List<Assunto> AssuntosExclusivosAlvoPendentes { get; set; } = new();

        // Exclusivos do Alvo mas já Concluídos (Raro, mas acontece)
        public List<Assunto> AssuntosExclusivosAlvoConcluidos { get; set; } = new();

        // Exclusivos do Base (O que você estuda que não cai no Alvo)
        public List<Assunto> AssuntosExclusivosBase { get; set; } = new();

        // --- MÉTRICAS (KPIs) ---
        public double PercentualCompatibilidade { get; private set; }
        public double PercentualJaConcluidoDoAlvo { get; private set; }

        // --- RADAR TEMPORAL ---
        public int DiasAteProvaBase { get; private set; }
        public int DiasAteProvaAlvo { get; private set; }
        public string MensagemEstrategicaTempo { get; private set; } = string.Empty;

        // Flags para a UI saber quem destacar
        public bool IsBasePrioritaria { get; private set; }
        public bool IsAlvoPrioritario { get; private set; }

        public void CalcularMetricas()
        {
            // Total de assuntos do Edital Alvo (B)
            int totalAssuntosAlvo = AssuntosConciliaveisPendentes.Count +
                                    AssuntosConciliaveisConcluidos.Count +
                                    AssuntosExclusivosAlvoPendentes.Count +
                                    AssuntosExclusivosAlvoConcluidos.Count;

            // Compatibilidade: Interseção / Total Alvo
            int totalComuns = AssuntosConciliaveisPendentes.Count + AssuntosConciliaveisConcluidos.Count;

            // Conclusão Real: Quanto do Alvo eu já tenho marcado como 'Concluído'?
            int totalConcluidoRelevante = AssuntosConciliaveisConcluidos.Count + AssuntosExclusivosAlvoConcluidos.Count;

            if (totalAssuntosAlvo > 0)
            {
                PercentualCompatibilidade = (double)totalComuns / totalAssuntosAlvo * 100;
                PercentualJaConcluidoDoAlvo = (double)totalConcluidoRelevante / totalAssuntosAlvo * 100;
            }
            else
            {
                PercentualCompatibilidade = 0;
                PercentualJaConcluidoDoAlvo = 0;
            }
        }

        public void CalcularPrioridadeTemporal()
        {
            var hoje = DateTime.Today;

            // VALIDAÇÃO 1: Datas padrão ou nulas
            if (EditalBase.DataProva == default || EditalBase.DataProva == DateTime.MinValue)
            {
                DiasAteProvaBase = -1; // Indica data não definida
                IsBasePrioritaria = false;
            }
            else
            {
                DiasAteProvaBase = (EditalBase.DataProva - hoje).Days;
            }

            if (EditalAlvo.DataProva == default || EditalAlvo.DataProva == DateTime.MinValue)
            {
                DiasAteProvaAlvo = -1; // Indica data não definida
                IsAlvoPrioritario = false;
            }
            else
            {
                DiasAteProvaAlvo = (EditalAlvo.DataProva - hoje).Days;
            }

            // LÓGICA DE DECISÃO com validações
            if (DiasAteProvaBase < 0 && DiasAteProvaAlvo < 0)
            {
                // Ambas datas inválidas ou no passado
                MensagemEstrategicaTempo = "Datas de prova não definidas. Adicione-as para obter recomendações estratégicas.";
            }
            else if (DiasAteProvaBase >= 0 && (DiasAteProvaBase < DiasAteProvaAlvo || DiasAteProvaAlvo < 0))
            {
                IsBasePrioritaria = true;
                MensagemEstrategicaTempo = $"⚠️ URGÊNCIA: A prova de {EditalBase.Orgao} é em {DiasAteProvaBase} dias (mais próxima). Foque nos exclusivos da esquerda!";
            }
            else if (DiasAteProvaAlvo >= 0)
            {
                IsAlvoPrioritario = true;
                MensagemEstrategicaTempo = $"⚠️ URGÊNCIA: A prova de {EditalAlvo.Orgao} é em {DiasAteProvaAlvo} dias (mais próxima). Foque nos exclusivos da direita!";
            }
            else
            {
                MensagemEstrategicaTempo = "Datas de prova não definidas ou inconsistentes.";
            }
        }
    }
}