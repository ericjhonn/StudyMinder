using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Models.DTOs;
using StudyMinder.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace StudyMinder.ViewModels
{
    public partial class ComparadorEditaisViewModel : BaseViewModel
    {
        private readonly ComparadorEditaisService _comparadorService;
        private readonly EditalService _editalService;
        private readonly INotificationService _notificationService;

        public ComparadorEditaisViewModel(
            ComparadorEditaisService comparadorService,
            EditalService editalService,
            INotificationService notificationService)
        {
            _comparadorService = comparadorService;
            _editalService = editalService;
            _notificationService = notificationService;

            Title = "Comparador de Editais";

            // Carregar lista inicial
            _ = CarregarEditaisAsync();
        }

        // --- PROPRIEDADES ---

        [ObservableProperty]
        private ObservableCollection<Edital> _listaEditais = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CompararCommand))]
        private Edital? _editalBaseSelecionado;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CompararCommand))]
        private Edital? _editalAlvoSelecionado;

        [ObservableProperty]
        private ResultadoComparacao? _resultado;

        [ObservableProperty]
        private bool _temResultado = false;

        // Dados para os Gráficos
        public ObservableCollection<GraficoChartData> DadosGraficoAfinidade { get; } = new();
        public ObservableCollection<GraficoChartData> DadosGraficoConquista { get; } = new();

        // --- COMANDOS ---

        [RelayCommand(CanExecute = nameof(PodeComparar))]
        private async Task CompararAsync()
        {
            if (EditalBaseSelecionado == null || EditalAlvoSelecionado == null) return;

            if (EditalBaseSelecionado.Id == EditalAlvoSelecionado.Id)
            {
                _notificationService.ShowWarning("Atenção", "Selecione dois editais diferentes para comparar.");
                return;
            }

            IsBusy = true;
            try
            {
                // Chama o serviço
                var resultado = await _comparadorService.CompararAsync(
                    EditalBaseSelecionado.Id,
                    EditalAlvoSelecionado.Id
                );

                Resultado = resultado;
                TemResultado = true;

                // Atualiza Gráficos
                AtualizarGraficos(resultado);
            }
            catch (System.Exception ex)
            {
                _notificationService.ShowError("Erro", $"Falha ao comparar editais: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool PodeComparar()
        {
            return EditalBaseSelecionado != null && EditalAlvoSelecionado != null;
        }

        [RelayCommand]
        private void LimparSelecao()
        {
            EditalBaseSelecionado = null;
            EditalAlvoSelecionado = null;
            TemResultado = false;
            Resultado = null;
            DadosGraficoAfinidade.Clear();
            DadosGraficoConquista.Clear();
            
            // Invalidar cache quando limpar seleção (user pode ter atualizado editais)
            _comparadorService.ClearCache();
        }

        // --- MÉTODOS AUXILIARES ---

        private async Task CarregarEditaisAsync()
        {
            IsBusy = true;
            try
            {
                var editais = await _editalService.ObterTodosAsync();
                ListaEditais.Clear();
                foreach (var edital in editais)
                {
                    ListaEditais.Add(edital);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void AtualizarGraficos(ResultadoComparacao res)
        {
            // 1. Gráfico de Afinidade (O quanto os editais se parecem)
            DadosGraficoAfinidade.Clear();

            int totalComuns = res.AssuntosConciliaveisPendentes.Count + res.AssuntosConciliaveisConcluidos.Count;
            int totalNovos = res.AssuntosExclusivosAlvoPendentes.Count + res.AssuntosExclusivosAlvoConcluidos.Count;

            // Fatia: Aproveitável
            if (totalComuns > 0)
            {
                DadosGraficoAfinidade.Add(new GraficoChartData
                {
                    Title = "Aproveitável",
                    Value = totalComuns,
                    Color = "#4CAF50" // Verde Material Design
                });
            }

            // Fatia: Novo Conteúdo
            if (totalNovos > 0)
            {
                DadosGraficoAfinidade.Add(new GraficoChartData
                {
                    Title = "Novo Conteúdo",
                    Value = totalNovos,
                    Color = "#9E9E9E" // Cinza (Neutro)
                });
            }

            // 2. Gráfico de Conquista (Status Real)
            DadosGraficoConquista.Clear();

            // Fatia A: Já Garantido (Comum + Concluído)
            if (res.AssuntosConciliaveisConcluidos.Count > 0)
            {
                DadosGraficoConquista.Add(new GraficoChartData
                {
                    Title = "Garantido",
                    Value = res.AssuntosConciliaveisConcluidos.Count,
                    Color = "#2E7D32" // Verde Escuro (Sucesso)
                });
            }

            // Fatia B: Ouro (Comum + Pendente) -> Alta Prioridade
            if (res.AssuntosConciliaveisPendentes.Count > 0)
            {
                DadosGraficoConquista.Add(new GraficoChartData
                {
                    Title = "Alta Prioridade",
                    Value = res.AssuntosConciliaveisPendentes.Count,
                    Color = "#FFC107" // Âmbar/Amarelo (Atenção)
                });
            }

            // Fatia C: Gap (Exclusivo Alvo + Pendente)
            if (res.AssuntosExclusivosAlvoPendentes.Count > 0)
            {
                DadosGraficoConquista.Add(new GraficoChartData
                {
                    Title = "Específico",
                    Value = res.AssuntosExclusivosAlvoPendentes.Count,
                    Color = "#D32F2F" // Vermelho (Falta fazer)
                });
            }
        }
    }
}