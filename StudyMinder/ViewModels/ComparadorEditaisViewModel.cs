using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Models.DTOs;
using StudyMinder.Services;
using System.Collections.ObjectModel;
using System.ComponentModel; // Necessário para ICollectionView
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data; // Necessário para CollectionViewSource

namespace StudyMinder.ViewModels
{
    public partial class ComparadorEditaisViewModel : BaseViewModel
    {
        private readonly ComparadorEditaisService _comparadorService;
        private readonly EditalService _editalService;
        private readonly INotificationService _notificationService;
        private readonly CicloEstudoService _cicloEstudoService; // Nova dependência

        public ComparadorEditaisViewModel(
            ComparadorEditaisService comparadorService,
            EditalService editalService,
            INotificationService notificationService,
            CicloEstudoService cicloEstudoService) // Injeção do Ciclo
        {
            _comparadorService = comparadorService;
            _editalService = editalService;
            _notificationService = notificationService;
            _cicloEstudoService = cicloEstudoService;

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

        // Propriedade específica para suportar o agrupamento na UI
        [ObservableProperty]
        private ICollectionView? _assuntosPendentesGrouped;

        // Adicione estas propriedades junto com _assuntosPendentesGrouped
        [ObservableProperty]
        private ICollectionView? _assuntosExclusivosBaseGrouped;

        [ObservableProperty]
        private ICollectionView? _assuntosExclusivosAlvoGrouped;

        

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

                // Processa o agrupamento visual para a ListView
                ProcessarAgrupamento(resultado);

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
            AssuntosPendentesGrouped = null; // Limpa o agrupamento
            DadosGraficoAfinidade.Clear();
            DadosGraficoConquista.Clear();

            // Invalidar cache quando limpar seleção
            _comparadorService.ClearCache();
        }

        // Comando para adicionar o assunto ao ciclo (VINCULADO AO BOTÃO NA VIEW)
        [RelayCommand]
        private async Task AdicionarCiclo(Assunto assunto)
        {
            if (assunto == null) return;

            try
            {
                // Adiciona com duração padrão de 50 minutos (pode ser ajustado)
                await _cicloEstudoService.AdicionarAoCicloAsync(assunto.Id, 50);

                _notificationService.ShowSuccess("Sucesso", $"'{assunto.Nome}' adicionado ao Ciclo de Estudos!");

                // Opcional: Aqui você poderia remover o item da lista visualmente se desejado
            }
            catch (System.Exception ex)
            {
                _notificationService.ShowError("Erro", $"Não foi possível adicionar ao ciclo: {ex.Message}");
            }
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

        /// <summary>
        /// Configura a CollectionView para agrupar por Disciplina
        /// </summary>
        private void ProcessarAgrupamento(ResultadoComparacao resultado)
        {
            // 1. Oportunidades (Já existente - OK)
            if (resultado?.AssuntosConciliaveisPendentes != null)
            {
                var view = CollectionViewSource.GetDefaultView(resultado.AssuntosConciliaveisPendentes);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("Disciplina.Nome"));
                AssuntosPendentesGrouped = view;
            }

            // 2. Exclusivos Base (CORREÇÃO AQUI)
            if (resultado?.AssuntosExclusivosBase != null)
            {
                // Filtramos aqui para pegar apenas os NÃO concluídos (!a.Concluido)
                var exclusivosBasePendentes = resultado.AssuntosExclusivosBase
                                                       .Where(a => !a.Concluido)
                                                       .ToList(); // Materializa a lista filtrada

                var viewBase = CollectionViewSource.GetDefaultView(exclusivosBasePendentes);
                viewBase.GroupDescriptions.Clear();
                viewBase.GroupDescriptions.Add(new PropertyGroupDescription("Disciplina.Nome"));
                AssuntosExclusivosBaseGrouped = viewBase;
            }

            // 3. Exclusivos Alvo (Já estava correto, pois a lista já vem filtrada do DTO)
            if (resultado?.AssuntosExclusivosAlvoPendentes != null)
            {
                var viewAlvo = CollectionViewSource.GetDefaultView(resultado.AssuntosExclusivosAlvoPendentes);
                viewAlvo.GroupDescriptions.Clear();
                viewAlvo.GroupDescriptions.Add(new PropertyGroupDescription("Disciplina.Nome"));
                AssuntosExclusivosAlvoGrouped = viewAlvo;
            }
        }

        private void AtualizarGraficos(ResultadoComparacao res)
        {
            // 1. Gráfico de Afinidade (O quanto os editais se parecem)
            DadosGraficoAfinidade.Clear();

            int totalComuns = res.AssuntosConciliaveisPendentes.Count + res.AssuntosConciliaveisConcluidos.Count;
            int totalNovos = res.AssuntosExclusivosAlvoPendentes.Count + res.AssuntosExclusivosAlvoConcluidos.Count;

            if (totalComuns > 0)
            {
                DadosGraficoAfinidade.Add(new GraficoChartData
                {
                    Title = "Aproveitável",
                    Value = totalComuns,
                    Color = "#4CAF50"
                });
            }

            if (totalNovos > 0)
            {
                DadosGraficoAfinidade.Add(new GraficoChartData
                {
                    Title = "Novo Conteúdo",
                    Value = totalNovos,
                    Color = "#9E9E9E"
                });
            }

            // 2. Gráfico de Conquista (Status Real)
            DadosGraficoConquista.Clear();

            if (res.AssuntosConciliaveisConcluidos.Count > 0)
            {
                DadosGraficoConquista.Add(new GraficoChartData
                {
                    Title = "Garantido",
                    Value = res.AssuntosConciliaveisConcluidos.Count,
                    Color = "#2E7D32"
                });
            }

            if (res.AssuntosConciliaveisPendentes.Count > 0)
            {
                DadosGraficoConquista.Add(new GraficoChartData
                {
                    Title = "Alta Prioridade",
                    Value = res.AssuntosConciliaveisPendentes.Count,
                    Color = "#FFC107"
                });
            }

            if (res.AssuntosExclusivosAlvoPendentes.Count > 0)
            {
                DadosGraficoConquista.Add(new GraficoChartData
                {
                    Title = "Específico",
                    Value = res.AssuntosExclusivosAlvoPendentes.Count,
                    Color = "#D32F2F"
                });
            }
        }
    }
}