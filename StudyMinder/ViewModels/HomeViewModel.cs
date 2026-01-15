using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Data;
using StudyMinder.Services;
using StudyMinder.Navigation;
using StudyMinder.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Wpf;

namespace StudyMinder.ViewModels
{
    public partial class HomeViewModel : BaseViewModel
    {
        private readonly StudyMinderContext _context;
        private readonly EstudoService _estudoService;
        private readonly NavigationService _navigationService;
        private readonly TipoEstudoService _tipoEstudoService;
        private readonly AssuntoService _assuntoService;
        private readonly DisciplinaService _disciplinaService;
        private readonly EstudoTransactionService _transactionService;
        private readonly RevisaoService _revisaoService;
        private readonly RevisaoNotificacaoService _revisaoNotificacaoService;
        private readonly INotificationService _notificationService;
        private readonly IConfigurationService _configurationService;

        [ObservableProperty]
        private int _questoesHoje;
        partial void OnQuestoesHojeChanged(int value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] QuestoesHoje alterado para: {value}");

        [ObservableProperty]
        private int _acertosHoje;
        partial void OnAcertosHojeChanged(int value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] AcertosHoje alterado para: {value}");

        [ObservableProperty]
        private int _errosHoje;
        partial void OnErrosHojeChanged(int value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] ErrosHoje alterado para: {value}");

        [ObservableProperty]
        private double _rendimentoHoje;
        partial void OnRendimentoHojeChanged(double value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] RendimentoHoje alterado para: {value}");

        [ObservableProperty]
        private double _horasEstudadasHoje;
        partial void OnHorasEstudadasHojeChanged(double value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] HorasEstudadasHoje alterado para: {value}");

        [ObservableProperty]
        private double _mediaHorasPorDia;
        partial void OnMediaHorasPorDiaChanged(double value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] MediaHorasPorDia alterado para: {value}");

        [ObservableProperty]
        private int _diasEstudados;
        partial void OnDiasEstudadosChanged(int value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] DiasEstudados alterado para: {value}");

        [ObservableProperty]
        private double _horasTotais;
        partial void OnHorasTotaisChanged(double value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] HorasTotais alterado para: {value}");

        [ObservableProperty]
        private int _totalAcertos;
        partial void OnTotalAcertosChanged(int value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] TotalAcertos alterado para: {value}");

        [ObservableProperty]
        private int _totalErros;
        partial void OnTotalErrosChanged(int value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] TotalErros alterado para: {value}");

        [ObservableProperty]
        private double _rendimentoGeral;
        partial void OnRendimentoGeralChanged(double value) => System.Diagnostics.Debug.WriteLine($"[DEBUG] RendimentoGeral alterado para: {value}");

        [ObservableProperty]
        private int _totalAssuntos;

        [ObservableProperty]
        private int _assuntosConcluidos;

        [ObservableProperty]
        private double _percentualConclusao;

        [ObservableProperty]
        private ObservableCollection<HeatmapDia> _heatmapDias = new();

        [ObservableProperty]
        private DateTime _dataSelecionada = DateTime.Today;

        [ObservableProperty]
        private string _textoDataSelecionada = "Hoje";

        [ObservableProperty]
        private ObservableCollection<PizzaChartData> _pizzaChartData = new();

        [ObservableProperty]
        private bool _temDadosPizza = false;

        [ObservableProperty]
        private ObservableCollection<SolidColorBrush> _pizzaChartColors = new();

        [ObservableProperty]
        private double _pizzaHorasEstudadas = 0;

        [ObservableProperty]
        private int _pizzaTotalQuestoes = 0;

        [ObservableProperty]
        private double _pizzaRendimento = 0;

        [ObservableProperty]
        private int _pizzaTotalPaginas = 0;

        // Novas propriedades para os componentes detalhados
        [ObservableProperty]
        private ObservableCollection<AssuntoRendimento> _assuntosMenorRendimento = new();

        [ObservableProperty]
        private bool _temAssuntosMenorRendimento = false;

        [ObservableProperty]
        private ObservableCollection<RevisaoProxima> _proximasRevisoes = new();

        [ObservableProperty]
        private bool _temProximasRevisoes = false;

        [ObservableProperty]
        private ObservableCollection<RevisaoProxima> _todasRevisoes = new();

        [ObservableProperty]
        private string _filtroRevisaoAtivo = "Tudo";

        [ObservableProperty]
        private ObservableCollection<AssuntoProgresso> _progressoAssuntos = new();

        [ObservableProperty]
        private MetasSemana _metasSemana = new();

        [ObservableProperty]
        private DateTime _semanaAtual = DateTime.Today;

        [ObservableProperty]
        private bool _modoRevisoesHoje = true; // true = Hoje, false = Atrasadas

        [ObservableProperty]
        private string _textoBotaoModoRevisao = "Ver Atrasadas";

        [ObservableProperty]
        private ProximaProva? _proximaProva;

        [ObservableProperty]
        private bool _temProximaProva = false;

        [ObservableProperty]
        private ObservableCollection<EditalCronograma> _proximosEventos = new();

        [ObservableProperty]
        private bool _temProximosEventos = false;

        [ObservableProperty]
        private Edital? _editalSelecionado;

        [ObservableProperty]
        private ObservableCollection<double> _questoesUltimos7Dias = new();

        [ObservableProperty]
        private ObservableCollection<string> _datasUltimos7Dias = new();

        [ObservableProperty]
        private ObservableCollection<double> _rendimentosUltimosPeriodos = new();

        // Propriedade para o gráfico de linhas
        [ObservableProperty]
        private PlotModel? _plotModel;

        // Propriedade para o gráfico de editais (RendimentoProva)
        [ObservableProperty]
        private PlotModel? _plotModelEditais;

        // Propriedade para armazenar dados dos editais para uso em tooltip
        [ObservableProperty]
        private List<Edital> _dadosEditaisGrafico = new();

        // Controlador para o gráfico de linhas
        [ObservableProperty]
        private PlotController _plotController = new();

        // Propriedades para o gráfico de pizza (Acertos vs Erros)
        [ObservableProperty]
        private int _acertosGrafico = 0;
        partial void OnAcertosGraficoChanged(int value)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] AcertosGrafico alterado para: {value}");
            OnPropertyChanged(nameof(QuestoesTotal));
        }

        [ObservableProperty]
        private int _errosGrafico = 0;
        partial void OnErrosGraficoChanged(int value)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ErrosGrafico alterado para: {value}");
            OnPropertyChanged(nameof(QuestoesTotal));
        }

        /// <summary>
        /// Total de questões (Acertos + Erros) para exibir no KPI
        /// </summary>
        public int QuestoesTotal => AcertosGrafico + ErrosGrafico;

        // Propriedades para filtro do gráfico de linhas
        [ObservableProperty]
        private string _filtroGraficoAtivo = "Dia";

        [ObservableProperty]
        private string _textoPeriodoGrafico = "Últimos 7 Dias";

        // Propriedades para filtro do gráfico de editais (RendimentoProva)
        [ObservableProperty]
        private string _filtroEditaisAtivo = "Tudo";

        // Propriedades para o painel de filtros
        [ObservableProperty]
        private bool _isFiltrosPanelVisible = false;

        partial void OnIsFiltrosPanelVisibleChanged(bool value)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] OnIsFiltrosPanelVisibleChanged: {value}");
        }

        [ObservableProperty]
        private DateTime? _dataFiltroInicio;

        [ObservableProperty]
        private DateTime? _dataFiltroFim;

        [ObservableProperty]
        private TipoEstudo? _tipoEstudoFiltro;

        [ObservableProperty]
        private Disciplina? _disciplinaFiltro;

        [ObservableProperty]
        private Assunto? _assuntoFiltro;

        [ObservableProperty]
        private bool _filtrarAssuntosConcluidos = false;

        [ObservableProperty]
        private ObservableCollection<TipoEstudo> _tiposEstudo = new();

        [ObservableProperty]
        private ObservableCollection<Disciplina> _disciplinas = new();

        [ObservableProperty]
        private ObservableCollection<Assunto> _assuntos = new();

        [ObservableProperty]
        private bool _filtrosAtivos = false;

        [ObservableProperty]
        private int _contadorFiltrosAtivos = 0;

        [RelayCommand]
        private async Task NavegarParaAnterior()
        {
            DataSelecionada = DataSelecionada.AddDays(-1);
            await CarregarPizzaChartAsync();
        }

        [RelayCommand]
        private async Task NavegarParaProximo()
        {
            DataSelecionada = DataSelecionada.AddDays(1);
            await CarregarPizzaChartAsync();
        }

        [RelayCommand]
        private async Task NavegarParaHoje()
        {
            DataSelecionada = DateTime.Today;
            await CarregarPizzaChartAsync();
        }

        [RelayCommand]
        private void FiltrarRevisoes(string filtro)
        {
            FiltroRevisaoAtivo = filtro;
            ProximasRevisoes.Clear();

            IEnumerable<RevisaoProxima> revisoesFiltradasEnumeravel = filtro switch
            {
                "Tudo" => TodasRevisoes,
                "Clássicas" => TodasRevisoes.Where(r =>
                    r.TipoRevisao == TipoRevisaoEnum.Classico24h ||
                    r.TipoRevisao == TipoRevisaoEnum.Classico7d ||
                    r.TipoRevisao == TipoRevisaoEnum.Classico30d ||
                    r.TipoRevisao == TipoRevisaoEnum.Classico90d ||
                    r.TipoRevisao == TipoRevisaoEnum.Classico120d ||
                    r.TipoRevisao == TipoRevisaoEnum.Classico180d),
                "4.2" => TodasRevisoes.Where(r => r.TipoRevisao == TipoRevisaoEnum.Ciclo42),
                _ => TodasRevisoes.Where(r => r.TipoRevisao == TipoRevisaoEnum.Ciclo42)
            };

            var revisoesFiltradasLista = revisoesFiltradasEnumeravel.Take(5).ToList();

            foreach (var revisao in revisoesFiltradasLista)
            {
                ProximasRevisoes.Add(revisao);
            }

            // Atualizar propriedade booleana para controlar visibilidade
            TemProximasRevisoes = ProximasRevisoes.Count > 0;
        }

        [RelayCommand]
        private async Task FiltrarGrafico(string filtro)
        {
            FiltroGraficoAtivo = filtro;
            TextoPeriodoGrafico = filtro switch
            {
                "Dia" => "Últimos 7 Dias",
                "Semana" => "Últimas 12 Semanas",
                "Mês" => "Últimos 24 Meses",
                _ => "Últimos 7 Dias"
            };

            await CarregarDadosGraficoAsync();
        }

        [RelayCommand]
        private async Task FiltrarGraficoEditais(string filtro)
        {
            FiltroEditaisAtivo = filtro;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] FiltrarGraficoEditais - Filtro selecionado: {filtro}");

            await CarregarGraficoEditaisRendimentoAsync(filtro);
        }

        [RelayCommand]
        private void InicializarFiltro()
        {
            // Aplica o filtro padrão "Tudo" quando a View é carregada
            FiltrarRevisoes("Tudo");
        }

        [RelayCommand]
        private void ToggleFiltrosPanel()
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleFiltrosPanel chamado");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] IsFiltrosPanelVisible ANTES: {IsFiltrosPanelVisible}");
            IsFiltrosPanelVisible = !IsFiltrosPanelVisible;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] IsFiltrosPanelVisible DEPOIS: {IsFiltrosPanelVisible}");
        }

        [RelayCommand]
        private async Task AplicarFiltros()
        {
            AtualizarIndicadorFiltros();
            await CarregarDadosAsync();
        }

        [RelayCommand]
        private async Task LimparFiltros()
        {
            DataFiltroInicio = null;
            DataFiltroFim = null;
            DisciplinaFiltro = null;
            AssuntoFiltro = null;
            TipoEstudoFiltro = null;
            FiltrarAssuntosConcluidos = false;
            AtualizarIndicadorFiltros();
            await CarregarDadosAsync();
        }

        private void AtualizarIndicadorFiltros()
        {
            // Contar quantos filtros estão ativos
            int contador = 0;
            if (DataFiltroInicio.HasValue) contador++;
            if (DataFiltroFim.HasValue) contador++;
            if (DisciplinaFiltro != null) contador++;
            if (AssuntoFiltro != null) contador++;
            if (TipoEstudoFiltro != null) contador++;
            if (FiltrarAssuntosConcluidos) contador++;

            ContadorFiltrosAtivos = contador;
            FiltrosAtivos = contador > 0;
        }

        partial void OnDisciplinaFiltroChanged(Disciplina? value)
        {
            AssuntoFiltro = null;

            // Carregar assuntos da disciplina selecionada de forma lazy
            if (value != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Carregando assuntos para disciplina: {value.Nome}");
                _ = CarregarAssuntosPorDisciplinaAsync(value.Id);
            }
            else
            {
                // Limpar assuntos se nenhuma disciplina for selecionada
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Assuntos.Clear();
                });
            }
        }

        private async Task CarregarAssuntosPorDisciplinaAsync(int disciplinaId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CarregarAssuntosPorDisciplinaAsync iniciado para disciplinaId: {disciplinaId}");
                var assuntos = await _assuntoService.ObterPorDisciplinaAsync(disciplinaId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Obtidos {assuntos.Count} assuntos do serviço");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Assuntos.Clear();
                    foreach (var assunto in assuntos)
                        Assuntos.Add(assunto);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Assuntos adicionados à coleção. Total: {Assuntos.Count}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Erro ao carregar assuntos: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task NavegarSemanaAnterior()
        {
            SemanaAtual = SemanaAtual.AddDays(-7);
            await CarregarMetasSemanaAsync();
        }

        [RelayCommand]
        private async Task NavegarSemanaProxima()
        {
            SemanaAtual = SemanaAtual.AddDays(7);
            await CarregarMetasSemanaAsync();
        }

        [RelayCommand]
        private async Task NavegarSemanaAtual()
        {
            SemanaAtual = DateTime.Today;
            await CarregarMetasSemanaAsync();
        }

        [RelayCommand]
        private async Task AlterarModoRevisao()
        {
            ModoRevisoesHoje = !ModoRevisoesHoje;
            TextoBotaoModoRevisao = ModoRevisoesHoje ? "Ver Atrasadas" : "Ver Hoje";
            await CarregarProximasRevisoesAsync();
        }

        [RelayCommand]
        private async Task ExibirEventosAtrasados()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Exibindo eventos atrasados");

                var hoje = DateTime.Today;
                var hojeTicks = hoje.Ticks;

                // Carregar eventos atrasados (data < hoje), não ignorados e não concluídos
                var eventosAtrasados = await _context.EditalCronograma
                    .Include(ec => ec.Edital)
                    .Where(ec =>
                        ec.DataEventoTicks < hojeTicks &&   // Eventos anteriores a hoje
                        !ec.Ignorado &&                      // Não ignorados
                        !ec.Concluido)                       // Não concluídos
                    .OrderByDescending(ec => ec.DataEventoTicks)  // Mais recentes primeiro (menos atrasados)
                    .Take(5)                                 // Limitar aos 5 mais recentes atrasados
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Eventos atrasados encontrados: {eventosAtrasados.Count}");

                // Limpar e atualizar coleção
                ProximosEventos.Clear();
                foreach (var evento in eventosAtrasados)
                {
                    evento.IsEditing = false;
                    ProximosEventos.Add(evento);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Evento atrasado adicionado: {evento.Evento} - {evento.DataEvento:dd/MM/yyyy}");
                }

                // Atualizar propriedade booleana para controlar visibilidade
                TemProximosEventos = ProximosEventos.Count > 0;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Total de eventos atrasados exibidos: {ProximosEventos.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro em ExibirEventosAtrasados: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private async Task ExibirEventosProximos()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Exibindo eventos próximos");

                var hoje = DateTime.Today;
                var hojeTicks = hoje.Ticks;

                // Carregar próximos eventos (data >= hoje), não ignorados e não concluídos
                var eventosProximos = await _context.EditalCronograma
                    .Include(ec => ec.Edital)
                    .Where(ec =>
                        ec.DataEventoTicks >= hojeTicks &&  // Eventos a partir de hoje
                        !ec.Ignorado &&                      // Não ignorados
                        !ec.Concluido)                       // Não concluídos
                    .OrderBy(ec => ec.DataEventoTicks)      // Próximos primeiro
                    .Take(5)                                 // Limitar aos próximos 5 eventos
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Eventos próximos encontrados: {eventosProximos.Count}");

                // Limpar e atualizar coleção
                ProximosEventos.Clear();
                foreach (var evento in eventosProximos)
                {
                    evento.IsEditing = false;
                    ProximosEventos.Add(evento);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Evento próximo adicionado: {evento.Evento} - {evento.DataEvento:dd/MM/yyyy}");
                }

                // Atualizar propriedade booleana para controlar visibilidade
                TemProximosEventos = ProximosEventos.Count > 0;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Total de eventos próximos exibidos: {ProximosEventos.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro em ExibirEventosProximos: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private async Task AtualizarEventoAsync(EditalCronograma? evento)
        {
            if (evento == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Atualizando evento: {evento.Evento} - Concluído: {evento.Concluido}, Ignorado: {evento.Ignorado}");

                // Atualizar no banco de dados
                _context.EditalCronograma.Update(evento);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Evento atualizado com sucesso no banco de dados");

                // Recarregar a lista de eventos (mantendo o filtro atual)
                // Se estava exibindo próximos, recarrega próximos
                // Se estava exibindo atrasados, recarrega atrasados
                await CarregarProximosEventosAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro em AtualizarEventoAsync: {ex.Message}\n{ex.StackTrace}");
                _notificationService.ShowError("Erro ao Atualizar", $"Erro ao atualizar evento: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task IniciarRevisaoAsync(RevisaoProxima? revisaoProxima)
        {
            if (revisaoProxima == null) return;

            try
            {
                // Obter dados completos do assunto
                var assuntos = await _assuntoService.ObterTodosAsync();
                var assuntoCompleto = assuntos.FirstOrDefault(a => a.Id == revisaoProxima.Assunto.Id);
                if (assuntoCompleto == null) return;

                // Obter disciplina
                var disciplinas = await _disciplinaService.ObterTodasAsync();
                var disciplina = disciplinas.FirstOrDefault(d => d.Id == assuntoCompleto.DisciplinaId);
                if (disciplina == null) return;

                // Carregar tipo de estudo "Revisão"
                var tipoRevisao = await _estudoService.ObterTipoEstudoPorNomeAsync("Revisão");
                if (tipoRevisao == null) return;

                // Criar ViewModel para edição
                var viewModel = new EditarEstudoViewModel(
                    _estudoService,
                    _tipoEstudoService,
                    _assuntoService,
                    _disciplinaService,
                    _transactionService,
                    _navigationService,
                    _revisaoService,
                    _notificationService,
                    _configurationService);

                viewModel.EstudoSalvo += async (sender, args) =>
                {
                    // Quando o estudo for salvo, recarregar todo o dashboard
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Estudo salvo detectado na Home via evento. Recarregando...");
                    await CarregarDadosAsync();
                };

                // Inicializar modo revisão (sem ser uma revisão agendada específica)
                await viewModel.InicializarModoRevisaoAsync(disciplina, assuntoCompleto, tipoRevisao, revisaoProxima.Id);

                // Navegar para a view de edição
                var view = new Views.ViewEstudoEditar { DataContext = viewModel };
                _navigationService.NavigateTo(view);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Iniciar", $"Erro ao iniciar revisão: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task IniciarEstudoAsync(AssuntoRendimento? assunto)
        {
            if (assunto == null) return;

            try
            {
                // Obter dados completos do assunto pelo nome
                var assuntos = await _assuntoService.ObterTodosAsync();
                var assuntoCompleto = assuntos.FirstOrDefault(a => a.Nome == assunto.Nome);
                if (assuntoCompleto == null) return;

                // Obter disciplina usando ObterTodasAsync e filtrando
                var disciplinas = await _disciplinaService.ObterTodasAsync();
                var disciplina = disciplinas.FirstOrDefault(d => d.Id == assuntoCompleto.DisciplinaId);
                if (disciplina == null) return;

                // Carregar tipo de estudo "Revisão"
                var tipoRevisao = await _estudoService.ObterTipoEstudoPorNomeAsync("Revisão");
                if (tipoRevisao == null) return;

                // Criar ViewModel para edição
                var viewModel = new EditarEstudoViewModel(
                    _estudoService,
                    _tipoEstudoService,
                    _assuntoService,
                    _disciplinaService,
                    _transactionService,
                    _navigationService,
                    _revisaoService,
                    _notificationService,
                    _configurationService);

                viewModel.EstudoSalvo += async (sender, args) =>
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Estudo salvo (Rendimento) detectado na Home. Recarregando...");
                    await CarregarDadosAsync();
                };

                // Inicializar modo revisão (sem ser uma revisão agendada)
                await viewModel.InicializarModoRevisaoAsync(disciplina, assuntoCompleto, tipoRevisao, 0);

                // Navegar para a view de edição
                var view = new Views.ViewEstudoEditar { DataContext = viewModel };
                _navigationService.NavigateTo(view);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Iniciar", $"Erro ao iniciar estudo: {ex.Message}");
            }
        }

        public HomeViewModel(
            StudyMinderContext context,
            EstudoService estudoService,
            NavigationService navigationService,
            TipoEstudoService tipoEstudoService,
            AssuntoService assuntoService,
            DisciplinaService disciplinaService,
            EstudoTransactionService transactionService,
            RevisaoService revisaoService,
            RevisaoNotificacaoService revisaoNotificacaoService,
            INotificationService notificationService,
            IConfigurationService configurationService)
        {
            _context = context;
            _estudoService = estudoService;
            _navigationService = navigationService;
            _tipoEstudoService = tipoEstudoService;
            _assuntoService = assuntoService;
            _disciplinaService = disciplinaService;
            _transactionService = transactionService;
            _revisaoService = revisaoService;
            _revisaoNotificacaoService = revisaoNotificacaoService;
            _notificationService = notificationService;
            _configurationService = configurationService;
            Title = "Dashboard";

            // Inscrever nos eventos de revisão para atualizar próximas revisões em tempo real
            _revisaoNotificacaoService.RevisaoAtualizada += OnRevisaoAtualizada;
            System.Diagnostics.Debug.WriteLine("[DEBUG] HomeViewModel inscrito em RevisaoAtualizada");
        }

        public async Task CarregarDadosAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Iniciando CarregarDadosAsync");
                IsBusy = true;

                // Carregar dados para filtros
                await CarregarDadosFiltrosAsync();

                await CarregarDadosHojeAsync();
                await CarregarDadosGeraisAsync();
                await CarregarDadosProgressoAsync();
                await CarregarHeatmapAsync();
                await CarregarPizzaChartAsync();
                await CarregarDadosDetalhadosAsync();
                await CarregarMetasSemanaAsync();
                await CarregarProximaProvaAsync();
                await CarregarDadosGraficoAsync();
                await CarregarGraficoAcertosErrosAsync();
                await CarregarGraficoEditaisRendimentoAsync();
                System.Diagnostics.Debug.WriteLine("[DEBUG] CarregarDadosAsync concluído com sucesso");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Constrói uma query base de estudos com os filtros aplicados.
        /// Útil para KPIs, gráficos e heatmaps que precisam respeitar filtros de data, tipo, disciplina e assunto.
        /// </summary>
        private IQueryable<Estudo> ConstruirQueryComFiltros()
        {
            var query = _context.Estudos
                .Include(e => e.Assunto)
                .ThenInclude(a => a.Disciplina)
                .AsQueryable();

            // Aplicar filtros
            if (DataFiltroInicio.HasValue)
            {
                var inicioTicks = DataFiltroInicio.Value.Date.Ticks;
                query = query.Where(e => e.DataTicks >= inicioTicks);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Filtro DataInicio aplicado: {DataFiltroInicio:dd/MM/yyyy}");
            }

            if (DataFiltroFim.HasValue)
            {
                var fimTicks = DataFiltroFim.Value.Date.AddDays(1).Ticks;
                query = query.Where(e => e.DataTicks < fimTicks);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Filtro DataFim aplicado: {DataFiltroFim:dd/MM/yyyy}");
            }

            if (TipoEstudoFiltro != null)
            {
                var tipoEstudoId = TipoEstudoFiltro.Id;
                query = query.Where(e => e.TipoEstudoId == tipoEstudoId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Filtro TipoEstudo aplicado: {TipoEstudoFiltro.Nome}");
            }

            if (DisciplinaFiltro != null)
            {
                var disciplinaId = DisciplinaFiltro.Id;
                query = query.Where(e => e.Assunto != null && e.Assunto.DisciplinaId == disciplinaId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Filtro Disciplina aplicado: {DisciplinaFiltro.Nome}");
            }

            if (AssuntoFiltro != null)
            {
                var assuntoId = AssuntoFiltro.Id;
                query = query.Where(e => e.AssuntoId == assuntoId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Filtro Assunto aplicado: {AssuntoFiltro.Nome}");
            }

            if (FiltrarAssuntosConcluidos)
            {
                query = query.Where(e => e.Assunto != null && e.Assunto.Concluido);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Filtro AssuntosConcluidos aplicado");
            }

            return query;
        }

        private async Task CarregarDadosFiltrosAsync()
        {
            // Apenas carregar se as coleções já não estiverem populadas.
            if (TiposEstudo.Any() || Disciplinas.Any())
            {
                return;
            }

            try
            {
                // Carregar dados para filtros (sem assuntos - carregamento lazy)
                var tiposTask = _tipoEstudoService.ObterAtivosAsync();
                var disciplinasTask = _disciplinaService.ObterTodasAsync();

                await Task.WhenAll(tiposTask, disciplinasTask);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TiposEstudo.Clear();
                    foreach (var tipo in tiposTask.Result)
                        TiposEstudo.Add(tipo);

                    Disciplinas.Clear();
                    foreach (var disciplina in disciplinasTask.Result)
                        Disciplinas.Add(disciplina);

                    // Assuntos não são carregados aqui - carregamento lazy ao selecionar disciplina
                    Assuntos.Clear();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Erro ao carregar dados de filtros: {ex.Message}");
            }
        }

        private async Task CarregarDadosHojeAsync()
        {
            // Usar intervalo de 24 horas começando do início do dia
            var hoje = DateTime.Today;
            var amanha = hoje.AddDays(1);
            var hojeTicks = hoje.Ticks;
            var amanhaTicks = amanha.Ticks;

            System.Diagnostics.Debug.WriteLine($"[DEBUG] CarregarDadosHojeAsync: Buscando estudos entre {hoje:dd/MM/yyyy HH:mm:ss} e {amanha:dd/MM/yyyy HH:mm:ss}");

            // Aplicar filtros de tipo, disciplina e assunto (sempre usar data de hoje)
            var query = _context.Estudos
                .Include(e => e.Assunto)
                .ThenInclude(a => a.Disciplina)
                .Where(e => e.DataTicks >= hojeTicks && e.DataTicks < amanhaTicks);

            // Aplicar filtros de tipo estudo, disciplina e assunto (sem filtro de data)
            if (TipoEstudoFiltro != null)
            {
                var tipoEstudoId = TipoEstudoFiltro.Id;
                query = query.Where(e => e.TipoEstudoId == tipoEstudoId);
            }

            if (DisciplinaFiltro != null)
            {
                var disciplinaId = DisciplinaFiltro.Id;
                query = query.Where(e => e.Assunto != null && e.Assunto.DisciplinaId == disciplinaId);
            }

            if (AssuntoFiltro != null)
            {
                var assuntoId = AssuntoFiltro.Id;
                query = query.Where(e => e.AssuntoId == assuntoId);
            }

            if (FiltrarAssuntosConcluidos)
            {
                query = query.Where(e => e.Assunto != null && e.Assunto.Concluido);
            }

            var estudosHoje = await query.ToListAsync();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Estudos encontrados hoje (com filtros): {estudosHoje.Count}");

            AcertosHoje = estudosHoje.Sum(e => e.Acertos);
            ErrosHoje = estudosHoje.Sum(e => e.Erros);
            QuestoesHoje = AcertosHoje + ErrosHoje;
            HorasEstudadasHoje = estudosHoje.Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours);
            RendimentoHoje = QuestoesHoje > 0 ? Math.Round((double)AcertosHoje / QuestoesHoje * 100, 2) : 0;
        }

        private async Task CarregarDadosGeraisAsync()
        {
            var query = ConstruirQueryComFiltros();
            var todosEstudos = await query.ToListAsync();

            TotalAcertos = todosEstudos.Sum(e => e.Acertos);
            TotalErros = todosEstudos.Sum(e => e.Erros);
            HorasTotais = todosEstudos.Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours);

            // Usar DataTicks para calcular dias distintos
            var diasDistintos = todosEstudos
                .Select(e => new DateTime(e.DataTicks).Date.Ticks)
                .Distinct()
                .Count();
            DiasEstudados = diasDistintos;
            MediaHorasPorDia = diasDistintos > 0 ? Math.Round(HorasTotais / diasDistintos, 2) : 0;

            var totalQuestoes = TotalAcertos + TotalErros;
            RendimentoGeral = totalQuestoes > 0 ? Math.Round((double)TotalAcertos / totalQuestoes * 100, 2) : 0;

            System.Diagnostics.Debug.WriteLine($"[DEBUG] CarregarDadosGeraisAsync: Acertos={TotalAcertos}, Erros={TotalErros}, HorasTotais={HorasTotais:F2}, DiasEstudados={DiasEstudados}");
        }

        private async Task CarregarDadosProgressoAsync()
        {
            try
            {
                IQueryable<Assunto> queryAssuntos = _context.Assuntos.AsQueryable();

                // Se um filtro de disciplina estiver ativo, o progresso é dentro dessa disciplina.
                if (DisciplinaFiltro != null)
                {
                    queryAssuntos = queryAssuntos.Where(a => a.DisciplinaId == DisciplinaFiltro.Id);
                }
                // Se um filtro de assunto estiver ativo, o progresso é 100% ou 0% para aquele assunto.
                else if (AssuntoFiltro != null)
                {
                    queryAssuntos = queryAssuntos.Where(a => a.Id == AssuntoFiltro.Id);
                }

                var assuntosParaCalculo = await queryAssuntos.ToListAsync();

                TotalAssuntos = assuntosParaCalculo.Count;
                AssuntosConcluidos = assuntosParaCalculo.Count(a => a.Concluido);
                PercentualConclusao = TotalAssuntos > 0 ? Math.Round((double)AssuntosConcluidos / TotalAssuntos * 100, 1) : 0;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] CarregarDadosProgressoAsync (FILTRADO): TotalAssuntos={TotalAssuntos}, AssuntosConcluidos={AssuntosConcluidos}, Percentual={PercentualConclusao:F1}%");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Erro ao carregar dados de progresso: {ex.Message}");
            }
        }

        private async Task CarregarHeatmapAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Iniciando CarregarHeatmapAsync com filtros aplicados");

                // Obter o mês e ano atual
                var agora = DateTime.Today;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Mês/Ano atual: {agora:yyyy-MM}");

                var primeiroDia = new DateTime(agora.Year, agora.Month, 1);
                var ultimoDia = primeiroDia.AddMonths(1).AddDays(-1);
                var proximoDia = ultimoDia.AddDays(1);
                var primeiroTicks = primeiroDia.Ticks;
                var proximoTicks = proximoDia.Ticks;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Intervalo: {primeiroDia:dd/MM/yyyy} a {ultimoDia:dd/MM/yyyy}");

                // Construir query com filtros e limite ao mês atual
                var query = ConstruirQueryComFiltros()
                    .Where(e => e.DataTicks >= primeiroTicks && e.DataTicks < proximoTicks);

                var estudosMes = await query.ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Total de estudos no mês (com filtros): {estudosMes.Count}");

                // Agrupar estudos por dia (em memória, após carregar do banco)
                var estudosPorDia = estudosMes
                    .GroupBy(e => new DateTime(e.DataTicks).Day)
                    .ToDictionary(g => g.Key, g => new
                    {
                        TemEstudo = true,
                        HorasEstudadas = g.Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours),
                        TotalQuestoes = g.Sum(e => e.Acertos + e.Erros)
                    });

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Dias com estudos: {(estudosPorDia.Count > 0 ? string.Join(", ", estudosPorDia.Keys.OrderBy(k => k)) : "nenhum")}");

                // Limpar coleção anterior
                HeatmapDias.Clear();

                // Criar heatmap para os 31 dias
                for (int dia = 1; dia <= 31; dia++)
                {
                    var heatmapDia = new HeatmapDia(dia);

                    if (estudosPorDia.ContainsKey(dia))
                    {
                        var dados = estudosPorDia[dia];
                        heatmapDia.TemEstudo = dados.TemEstudo;
                        heatmapDia.HorasEstudadas = dados.HorasEstudadas;
                        heatmapDia.TotalQuestoes = dados.TotalQuestoes;
                    }

                    HeatmapDias.Add(heatmapDia);
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Heatmap carregado com {HeatmapDias.Count} dias totais");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Heatmap carregado com {HeatmapDias.Count(d => d.TemEstudo)} dias com estudos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro ao carregar heatmap: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
            }
        }

        private async Task CarregarPizzaChartAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Carregando pizza chart para data: {DataSelecionada:dd/MM/yyyy}");

                var dataInicio = DataSelecionada.Date;
                var dataFim = dataInicio.AddDays(1);
                var dataInicioTicks = dataInicio.Ticks;
                var dataFimTicks = dataFim.Ticks;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Intervalo: {dataInicio:dd/MM/yyyy HH:mm:ss} a {dataFim:dd/MM/yyyy HH:mm:ss}");

                // Obter estudos do dia selecionado
                var estudosDia = await _context.Estudos
                    .Where(e => e.DataTicks >= dataInicioTicks && e.DataTicks < dataFimTicks)
                    .Include(e => e.TipoEstudo)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Total de estudos no dia: {estudosDia.Count}");

                // Agrupar por TipoEstudo
                var estudosPorTipo = estudosDia
                    .Where(e => e.TipoEstudo != null)
                    .GroupBy(e => e.TipoEstudo!)
                    .Select(g => new
                    {
                        TipoEstudo = g.Key,
                        Quantidade = g.Count(),
                        Horas = g.Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours)
                    })
                    .OrderByDescending(x => x.Quantidade)
                    .ToList();

                // Limpar dados anteriores
                PizzaChartData.Clear();
                PizzaChartColors.Clear();

                if (estudosPorTipo.Any())
                {
                    int totalEstudos = estudosPorTipo.Sum(x => x.Quantidade);
                    double totalHoras = 0;
                    int totalQuestoes = 0;
                    int totalPaginas = 0;
                    int totalAcertos = 0;

                    // Primeiro, adicionar todas as cores
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Adicionando cores para {estudosPorTipo.Count} tipos");
                    foreach (var item in estudosPorTipo)
                    {
                        var brush = TipoEstudoColorMap.GetBrush(item.TipoEstudo.Nome);
                        PizzaChartColors.Add(brush);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Adicionada cor para {item.TipoEstudo.Nome}. Total de cores: {PizzaChartColors.Count}");
                    }

                    // Depois, adicionar os dados
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Adicionando dados para {estudosPorTipo.Count} tipos");
                    foreach (var item in estudosPorTipo)
                    {
                        var pizzaData = new PizzaChartData(
                            item.TipoEstudo.Nome,
                            item.Quantidade,
                            totalEstudos
                        );

                        PizzaChartData.Add(pizzaData);
                        totalHoras += item.Horas;

                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Tipo: {item.TipoEstudo.Nome}, Qtd: {item.Quantidade}, Percentual: {pizzaData.Percentual:F1}%");
                    }

                    // Calcular dados da legenda
                    totalQuestoes = estudosDia.Sum(e => e.Acertos + e.Erros);
                    totalAcertos = estudosDia.Sum(e => e.Acertos);
                    totalPaginas = estudosDia.Sum(e => e.TotalPaginas);

                    PizzaHorasEstudadas = totalHoras;
                    PizzaTotalQuestoes = totalQuestoes;
                    PizzaTotalPaginas = totalPaginas;
                    PizzaRendimento = totalQuestoes > 0 ? Math.Round((double)totalAcertos / totalQuestoes * 100, 1) : 0;

                    TemDadosPizza = true;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Pizza chart carregado com {PizzaChartData.Count} categorias");
                }
                else
                {
                    TemDadosPizza = false;
                    PizzaHorasEstudadas = 0;
                    PizzaTotalQuestoes = 0;
                    PizzaTotalPaginas = 0;
                    PizzaRendimento = 0;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Nenhum estudo encontrado para a data selecionada");
                }

                // Atualizar texto da data
                AtualizarTextoDataSelecionada();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro ao carregar pizza chart: {ex.Message}");
                TemDadosPizza = false;
                PizzaChartData.Clear();
                PizzaChartColors.Clear();
            }
        }

        private void AtualizarTextoDataSelecionada()
        {
            if (DataSelecionada == DateTime.Today)
            {
                TextoDataSelecionada = "Hoje";
            }
            else if (DataSelecionada == DateTime.Today.AddDays(-1))
            {
                TextoDataSelecionada = "Ontem";
            }
            else
            {
                TextoDataSelecionada = DataSelecionada.ToString("dd/MM/yyyy");
            }
        }

        private async Task CarregarDadosDetalhadosAsync()
        {
            try
            {
                // 1. Carregar 5 assuntos com menor rendimento
                await CarregarAssuntosMenorRendimentoAsync();

                // 2. Carregar próximas 5 revisões
                await CarregarProximasRevisoesAsync();

                // 3. Carregar próximos eventos do cronograma
                await CarregarProximosEventosAsync();

                // 4. Carregar progresso de conclusão dos assuntos (removido daqui, pois já está no CarregarDadosAsync principal)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro ao carregar dados detalhados: {ex.Message}");
            }
        }

        private async Task CarregarAssuntosMenorRendimentoAsync()
        {
            try
            {
                // Usar a query com filtros como base
                var query = ConstruirQueryComFiltros();

                var estudosFiltrados = await query
                    .Include(e => e.Assunto)
                    .ThenInclude(a => a.Disciplina)
                    .ToListAsync();

                var assuntosRendimento = estudosFiltrados
                    .Where(e => e.Assunto != null) // Garantir que o assunto não seja nulo
                    .GroupBy(e => e.Assunto) // Agrupar por objeto Assunto
                    .Select(g => new AssuntoRendimento
                    {
                        Nome = g.Key.Nome,
                        Disciplina = g.Key.Disciplina,
                        TotalAcertos = g.Sum(e => e.Acertos),
                        TotalErros = g.Sum(e => e.Erros),
                        TotalQuestoes = g.Sum(e => e.Acertos + e.Erros),
                        HorasEstudadas = g.Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours),
                        Concluido = g.Key.Concluido,
                        RendimentoPercentual = g.Sum(e => e.Acertos + e.Erros) > 0
                            ? Math.Round((double)g.Sum(e => e.Acertos) / g.Sum(e => e.Acertos + e.Erros) * 100, 2)
                            : 0
                    })
                    .Where(a => a.TotalQuestoes > 0) // Apenas assuntos com questões respondidas no período filtrado
                    .OrderBy(a => a.RendimentoPercentual)
                    .Take(5)
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    AssuntosMenorRendimento.Clear();
                    foreach (var assunto in assuntosRendimento)
                    {
                        AssuntosMenorRendimento.Add(assunto);
                    }
                    TemAssuntosMenorRendimento = AssuntosMenorRendimento.Count > 0;
                });

                System.Diagnostics.Debug.WriteLine($"[DEBUG] CarregarAssuntosMenorRendimentoAsync (FILTRADO): Encontrados {AssuntosMenorRendimento.Count} assuntos.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Erro ao carregar assuntos de menor rendimento: {ex.Message}");
            }
        }

        private async Task CarregarProximasRevisoesAsync()
        {
            try
            {
                //var hoje = DateTime.Today;

                // Carregar todas as revisões não concluídas (EstudoRealizadoId == null)
                var todasRevisoesDoBanco = await _context.Revisoes
                    .Include(r => r.EstudoOrigem)
                    .ThenInclude(e => e.Assunto)
                    .ThenInclude(a => a.Disciplina)
                    .Where(r => r.EstudoRealizadoId == null) // Apenas revisões não concluídas
                    .OrderBy(r => r.DataProgramadaTicks)
                    .Take(5) // Limitar a 5 para performance
                    .ToListAsync();

                //// Filtrar conforme o modo de data ("Hoje" vs "Atrasadas")
                //IEnumerable<Revisao> revisoesFiltradasPorData;
                //if (ModoRevisoesHoje)
                //{
                //    // Modo "Hoje": revisões agendadas para hoje ou no futuro
                //    revisoesFiltradasPorData = todasRevisoesDoBanco.Where(r => r.DataProgramada.Date >= hoje);
                //}
                //else
                //{
                //    // Modo "Atrasadas": revisões com data programada anterior a hoje, as mais recentes primeiro
                //    revisoesFiltradasPorData = todasRevisoesDoBanco.Where(r => r.DataProgramada.Date < hoje).OrderByDescending(r => r.DataProgramadaTicks);
                //}

                // Carregar o resultado do filtro de data em TodasRevisoes
                TodasRevisoes.Clear();
                foreach (var revisao in todasRevisoesDoBanco)
                {
                    TodasRevisoes.Add(new RevisaoProxima
                    {
                        Id = revisao.Id,
                        DataRevisao = revisao.DataProgramada,
                        Assunto = revisao.EstudoOrigem.Assunto,
                        TipoRevisao = revisao.TipoRevisao
                    });
                }

                // Aplicar o filtro de TIPO (Tudo, Clássicas, 4.2) sobre a lista já filtrada por DATA
                FiltrarRevisoes(FiltroRevisaoAtivo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro em CarregarProximasRevisoesAsync: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task CarregarProximaProvaAsync()
        {
            try
            {
                var hoje = DateTime.Today;
                var hojeTicks = hoje.Ticks;

                // Buscar o edital não arquivado com a data de prova mais próxima
                var proximaProva = await _context.Editais
                    .Where(e => !e.Arquivado && e.DataProvaTicks >= hojeTicks)
                    .OrderBy(e => e.DataProvaTicks)
                    .FirstOrDefaultAsync();

                if (proximaProva != null)
                {
                    var diasParaProva = (proximaProva.DataProva - hoje).Days;

                    ProximaProva = new ProximaProva
                    {
                        DataProva = proximaProva.DataProva,
                        Orgao = proximaProva.Orgao,
                        Cargo = proximaProva.Cargo,
                        DiasParaProva = diasParaProva
                    };

                    TemProximaProva = true;
                }
                else
                {
                    ProximaProva = null;
                    TemProximaProva = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro em CarregarProximaProvaAsync: {ex.Message}");
                ProximaProva = null;
                TemProximaProva = false;
            }
        }

        private async Task CarregarProximosEventosAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Iniciando CarregarProximosEventosAsync");

                var hoje = DateTime.Today;
                var hojeTicks = hoje.Ticks;

                // Carregar próximos eventos do cronograma de editais
                // Buscar eventos não ignorados e não concluídos, ordenados por data
                var proximosEventos = await _context.EditalCronograma
                    .Include(ec => ec.Edital)
                    .Where(ec =>
                        ec.DataEventoTicks >= hojeTicks &&  // Eventos a partir de hoje
                        !ec.Ignorado &&                      // Não ignorados
                        !ec.Concluido)                       // Não concluídos
                    .OrderBy(ec => ec.DataEventoTicks)      // Ordenar por data
                    .Take(5)                                 // Limitar aos próximos 5 eventos
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Próximos eventos encontrados: {proximosEventos.Count}");

                // Limpar coleção anterior
                ProximosEventos.Clear();

                // Adicionar eventos à coleção
                foreach (var evento in proximosEventos)
                {
                    evento.IsEditing = false;
                    ProximosEventos.Add(evento);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Evento adicionado: {evento.Evento} - {evento.DataEvento:dd/MM/yyyy}");
                }

                // Atualizar propriedade booleana para controlar visibilidade
                TemProximosEventos = ProximosEventos.Count > 0;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] CarregarProximosEventosAsync concluído. Total de eventos: {ProximosEventos.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro em CarregarProximosEventosAsync: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task CarregarProgressoAssuntosAsync()
        {
            var assuntos = await _context.Assuntos
                .Include(a => a.Estudos)
                .ToListAsync();

            var progressoAssuntos = assuntos
                .Select(a => new AssuntoProgresso
                {
                    Nome = a.Nome,
                    Concluido = a.Concluido,
                    TotalAcertos = a.Estudos.Sum(e => e.Acertos),
                    TotalErros = a.Estudos.Sum(e => e.Erros),
                    HorasEstudadas = a.Estudos.Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours),
                    PercentualConclusao = a.Concluido ? 100.0 :
                        (a.Estudos.Any() ? Math.Round((double)a.Estudos.Sum(e => e.Acertos) /
                            Math.Max(a.Estudos.Sum(e => e.Acertos + e.Erros), 1) * 100, 1) : 0.0)
                })
                .OrderBy(a => a.Nome)
                .ToList();

            ProgressoAssuntos.Clear();
            foreach (var assunto in progressoAssuntos)
            {
                ProgressoAssuntos.Add(assunto);
            }
        }

        private async Task CarregarMetasSemanaAsync()
        {
            try
            {
                // Calcular início e fim da semana (segunda a domingo)
                var dataInicio = SemanaAtual.AddDays(-(int)SemanaAtual.DayOfWeek + 1); // Segunda-feira
                if (dataInicio > SemanaAtual) dataInicio = dataInicio.AddDays(-7); // Ajuste se necessário
                var dataFim = dataInicio.AddDays(6); // Domingo

                var dataInicioTicks = dataInicio.Ticks;
                var dataFimTicks = dataFim.AddDays(1).Ticks; // Incluir até o final do domingo

                // Obter estudos da semana
                var estudosSemana = await _context.Estudos
                    .Where(e => e.DataTicks >= dataInicioTicks && e.DataTicks < dataFimTicks)
                    .ToListAsync();

                // Calcular totalizações
                var horasRealizadas = estudosSemana.Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours);
                var questoesRealizadas = estudosSemana.Sum(e => e.Acertos + e.Erros);
                var paginasRealizadas = estudosSemana.Sum(e => e.TotalPaginas);

                // Obter metas das configurações
                var settings = App.ConfigurationService.Settings;
                var metaHoras = settings.Goals.WeeklyStudyHoursGoal;
                var metaQuestoes = settings.Goals.WeeklyQuestionsGoal;
                var metaPaginas = settings.Goals.WeeklyPagesGoal;

                // Atualizar modelo
                MetasSemana = new MetasSemana
                {
                    DataInicio = dataInicio,
                    DataFim = dataFim,
                    MetaHoras = metaHoras,
                    MetaQuestoes = metaQuestoes,
                    MetaPaginas = metaPaginas,
                    HorasRealizadas = horasRealizadas,
                    QuestoesRealizadas = questoesRealizadas,
                    PaginasRealizadas = paginasRealizadas
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro em CarregarMetasSemanaAsync: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task CarregarDadosGraficoAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Iniciando CarregarDadosGraficoAsync - Filtro: {FiltroGraficoAtivo}");

                // Calcular período baseado no filtro
                var dataFim = DateTime.Today;
                DateTime dataInicio;
                string formatoSaida;

                switch (FiltroGraficoAtivo)
                {
                    case "Semana":
                        // Últimas 12 semanas (3 meses)
                        dataInicio = dataFim.AddDays(-7 * 12);
                        formatoSaida = "dd/MMM";
                        break;
                    case "Mês":
                        // Últimos 24 meses (2 anos)
                        dataInicio = dataFim.AddMonths(-24);
                        formatoSaida = "MM/yyyy";
                        break;
                    default: // "Dia"
                        // Últimos 7 dias
                        dataInicio = dataFim.AddDays(-7);
                        formatoSaida = "dd/MMM";
                        break;
                }

                var dataInicioTicks = dataInicio.Ticks;
                var proximoDia = dataFim.AddDays(1);
                var dataFimTicks = proximoDia.Ticks;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Período: {dataInicio:dd/MM/yyyy HH:mm:ss} a {dataFim:dd/MM/yyyy HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Ticks: {dataInicioTicks} a {dataFimTicks}");

                // Obter estudos do período COM FILTROS aplicados
                var query = ConstruirQueryComFiltros()
                    .Where(e => e.DataTicks >= dataInicioTicks && e.DataTicks < dataFimTicks);

                var estudos = await query.ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Estudos encontrados (com filtros): {estudos.Count}");

                // Agrupar dados baseado no filtro
                var dadosAgrupados = new List<DateTime>();
                var valoresAgrupados = new List<double>();
                var rendimentosAgrupados = new List<double>();

                switch (FiltroGraficoAtivo)
                {
                    case "Semana":
                        // Agrupar por semana
                        var dadosPorSemana = estudos
                            .GroupBy(e =>
                            {
                                var data = new DateTime(e.DataTicks);
                                // Encontrar o início da semana (domingo)
                                var diasDesdeDomingo = (int)data.DayOfWeek;
                                return data.AddDays(-diasDesdeDomingo).Date;
                            })
                            .OrderBy(g => g.Key)
                            .ToList();

                        foreach (var grupo in dadosPorSemana.TakeLast(12)) // Últimas 12 semanas
                        {
                            dadosAgrupados.Add(grupo.Key);
                            var totalAcertos = grupo.Sum(e => e.Acertos);
                            var totalErros = grupo.Sum(e => e.Erros);
                            var totalQuestoes = totalAcertos + totalErros;

                            valoresAgrupados.Add(totalQuestoes);

                            // Calcular rendimento: TotalAcertos / (TotalAcertos + TotalErros)
                            var rendimento = totalQuestoes > 0 ? (double)totalAcertos / totalQuestoes * 100 : 0;
                            rendimentosAgrupados.Add(rendimento);
                        }
                        break;

                    case "Mês":
                        // Agrupar por mês
                        var dadosPorMes = estudos
                            .GroupBy(e =>
                            {
                                var data = new DateTime(e.DataTicks);
                                return new DateTime(data.Year, data.Month, 1);
                            })
                            .OrderBy(g => g.Key)
                            .ToList();

                        foreach (var grupo in dadosPorMes.TakeLast(24)) // Últimos 24 meses
                        {
                            dadosAgrupados.Add(grupo.Key);
                            var totalAcertos = grupo.Sum(e => e.Acertos);
                            var totalErros = grupo.Sum(e => e.Erros);
                            var totalQuestoes = totalAcertos + totalErros;

                            valoresAgrupados.Add(totalQuestoes);

                            // Calcular rendimento: TotalAcertos / (TotalAcertos + TotalErros)
                            var rendimento = totalQuestoes > 0 ? (double)totalAcertos / totalQuestoes * 100 : 0;
                            rendimentosAgrupados.Add(rendimento);
                        }
                        break;

                    default: // "Dia"
                        // Agrupar por dia
                        var dadosPorDia = estudos
                            .GroupBy(e =>
                            {
                                var data = new DateTime(e.DataTicks);
                                return data.Date;
                            })
                            .OrderBy(g => g.Key)
                            .ToList();

                        foreach (var grupo in dadosPorDia.TakeLast(7)) // Últimos 7 dias
                        {
                            dadosAgrupados.Add(grupo.Key);
                            var totalAcertos = grupo.Sum(e => e.Acertos);
                            var totalErros = grupo.Sum(e => e.Erros);
                            var totalQuestoes = totalAcertos + totalErros;

                            valoresAgrupados.Add(totalQuestoes);

                            // Calcular rendimento: TotalAcertos / (TotalAcertos + TotalErros)
                            var rendimento = totalQuestoes > 0 ? (double)totalAcertos / totalQuestoes * 100 : 0;
                            rendimentosAgrupados.Add(rendimento);
                        }
                        break;
                }

                // Atualizar collections
                QuestoesUltimos7Dias.Clear();
                DatasUltimos7Dias.Clear();
                RendimentosUltimosPeriodos.Clear();

                foreach (var valor in valoresAgrupados)
                {
                    QuestoesUltimos7Dias.Add(valor);
                }

                foreach (var data in dadosAgrupados)
                {
                    DatasUltimos7Dias.Add(data.ToString(formatoSaida));
                }

                foreach (var rendimento in rendimentosAgrupados)
                {
                    RendimentosUltimosPeriodos.Add(rendimento);
                }

                // Configurar gráfico
                //ConfigurarGraficoLinhas(valoresAgrupados.ToList(), dadosAgrupados.Select(d => d.ToString(formatoSaida)).ToList(), rendimentosAgrupados.ToList());
                ConfigurarGraficoLinhas(valoresAgrupados.ToList(), dadosAgrupados, rendimentosAgrupados.ToList());

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Dados carregados - Valores: {string.Join(", ", valoresAgrupados)}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Datas: {string.Join(", ", dadosAgrupados.Select(d => d.ToString(formatoSaida)))}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Rendimentos: {string.Join(", ", rendimentosAgrupados.Select(r => r.ToString("F1") + "%"))}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro em CarregarDadosGraficoAsync: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Note que alterei a assinatura: List<string> datas virou List<DateTime> datas
        private void ConfigurarGraficoLinhas(List<double> questoes, List<DateTime> datas, List<double> rendimentos)
        {
            try
            {
                // Validações básicas mantidas
                if (questoes?.Count != datas?.Count || questoes?.Count != rendimentos?.Count) return;

                var plotModel = new PlotModel
                {
                    Background = OxyColors.Transparent,
                    PlotAreaBorderThickness = new OxyThickness(0), // Remove a borda quadrada ao redor do gráfico
                    PlotMargins = new OxyThickness(0, 10, 0, 30)   // Margens ajustadas para não cortar o texto do eixo X
                };

                // ---------------------------------------------------------
                // 1. Eixo X (TEMPO) - A grande mudança
                // ---------------------------------------------------------
                // Usamos DateTimeAxis. Isso permite zoom infinito e o texto se ajusta sozinho.
                var xAxis = new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    StringFormat = "dd/MMM", // Formato da data

                    // Estilo Minimalista
                    AxislineStyle = LineStyle.Solid,
                    AxislineColor = OxyColors.Gray,
                    TickStyle = TickStyle.None, // Remove os tracinhos da régua

                    // Grid
                    MajorGridlineStyle = LineStyle.None,
                    MinorGridlineStyle = LineStyle.None,

                    // Cor do texto
                    TextColor = OxyColor.FromRgb(150, 150, 150),
                    FontSize = 11,

                    // Garante que haja um pequeno respiro nas laterais para o primeiro/último ponto
                    IntervalType = DateTimeIntervalType.Auto,
                    MinimumPadding = 0.05,
                    MaximumPadding = 0.05
                };
                plotModel.Axes.Add(xAxis);

                // ---------------------------------------------------------
                // 2. Eixo Y Esquerdo (QUESTÕES) - Invisível mas estrutural
                // ---------------------------------------------------------
                var yAxisQuestoes = new LinearAxis
                {
                    Key = "QuestoesAxis",
                    Position = AxisPosition.Left,
                    LabelFormatter = _ => string.Empty, // Mantendo sua correção anterior

                    // Ajuste para Barras:
                    Minimum = 0,           // Força o chão em zero absoluto
                    MinimumPadding = 0,    // Remove o espaço extra abaixo do zero

                    MaximumPadding = 0.2,  // Dá um respiro bom no topo para o texto não cortar

                    IsAxisVisible = true,
                    TickStyle = TickStyle.None,
                    AxislineStyle = LineStyle.None,
                    MajorGridlineStyle = LineStyle.None,
                    MinorGridlineStyle = LineStyle.None
                };
                plotModel.Axes.Add(yAxisQuestoes);

                // ---------------------------------------------------------
                // 3. Eixo Y Direito (RENDIMENTO) - Funcional mas Limpo
                // ---------------------------------------------------------
                var yAxisRendimentos = new LinearAxis
                {
                    Key = "RendimentoAxis",
                    Position = AxisPosition.Right,

                    // Intervalo fixo 0-100%
                    Minimum = 0,
                    Maximum = 100,

                    // AQUI ESTÁ A CORREÇÃO DO SEU PROBLEMA:
                    // O eixo existe e funciona o zoom, mas não desenha nada.
                    LabelFormatter = _ => string.Empty,
                    TickStyle = TickStyle.None,
                    AxislineStyle = LineStyle.None,
                    MajorGridlineStyle = LineStyle.None,

                    IsZoomEnabled = true,
                    IsPanEnabled = true
                };
                plotModel.Axes.Add(yAxisRendimentos);

                // ---------------------------------------------------------
                // 4. Série de Questões (AGORA COMO BARRAS)
                // Define a largura baseada na granularidade dos dados
                double larguraBarra;

                // Você pode usar sua variável 'FiltroGraficoAtivo' ou inferir pelo intervalo de datas
                if (FiltroGraficoAtivo == "Mês")
                {
                    larguraBarra = 20.0; // Uma barra ocupa ~20 dias (deixa folga entre meses)
                }
                else if (FiltroGraficoAtivo == "Semana")
                {
                    larguraBarra = 20.0;  // Uma barra ocupa 5 dias (deixa 2 dias de folga na semana)
                }
                else // "Dia"
                {
                    larguraBarra = 20.0;  // Ocupa 80% do dia
                }

                // ---------------------------------------------------------
                // Usamos LinearBarSeries para que funcione com o DateTimeAxis
                var barSeriesQuestoes = new LinearBarSeries
                {
                    Title = "Questões",

                    // Visual Flat e Moderno
                    FillColor = OxyColor.FromRgb(100, 181, 246), // Azul
                    StrokeColor = OxyColors.Transparent, // Sem borda na barra
                    StrokeThickness = 0,

                    // LARGURA DA BARRA:
                    // Como o eixo X é DateTime (onde 1 unidade = 1 dia),
                    BarWidth = larguraBarra,

                    // Vincula ao eixo Y da esquerda
                    YAxisKey = "QuestoesAxis",

                    // Rótulos inteligentes (aparecem no topo da barra)
                    // Configura o texto que aparece ao passar o mouse (Tooltip)
                    // {0} = Título da Série
                    // {2} = Valor X (Data)
                    // {4} = Valor Y (Quantidade)
                    TrackerFormatString = "{0}\n{2:dd/MMM}: {4:0} questões",
                    TextColor = OxyColor.FromRgb(80, 140, 200), // Um azul um pouco mais escuro para o texto
                    FontSize = 11
                };

                // Populando a série de barras
                for (int i = 0; i < datas.Count; i++)
                {
                    // Note que usamos DataPoint para LinearBarSeries também
                    barSeriesQuestoes.Points.Add(new DataPoint(DateTimeAxis.ToDouble(datas[i]), questoes[i]));
                }

                // IMPORTANTE: Adicione as barras PRIMEIRO para ficarem no fundo
                plotModel.Series.Add(barSeriesQuestoes);


                // ---------------------------------------------------------
                // 5. Série de Rendimento (LINHA)
                // ---------------------------------------------------------
                // Mantenha o código da linha de rendimento aqui...
                var lineSeriesRendimentos = new LineSeries
                {
                    // ... (mesmo código anterior da linha roxa)
                    Title = "Rendimento",
                    Color = OxyColor.FromRgb(186, 104, 200),
                    StrokeThickness = 3,
                    MarkerType = MarkerType.Diamond,
                    MarkerSize = 4,
                    MarkerStroke = OxyColor.FromRgb(186, 104, 200),
                    MarkerFill = OxyColors.White,
                    MarkerStrokeThickness = 2,
                    YAxisKey = "RendimentoAxis",
                    LabelFormatString = "{1:0}%",
                    LabelMargin = 8,
                    TextColor = OxyColor.FromRgb(186, 104, 200),
                    FontSize = 11
                };

                for (int i = 0; i < datas.Count; i++)
                {
                    lineSeriesRendimentos.Points.Add(new DataPoint(DateTimeAxis.ToDouble(datas[i]), rendimentos[i]));
                }

                // A linha é adicionada DEPOIS, para ser desenhada SOBRE as barras
                plotModel.Series.Add(lineSeriesRendimentos);

                // Atribuição Final
                PlotModel = plotModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Erro ConfigurarGrafico]: {ex.Message}");
            }
        }

        /// <summary>
        /// Carrega o gráfico de RendimentoProva dos editais, ordenados por data da prova
        /// </summary>
        private async Task CarregarGraficoEditaisRendimentoAsync(string filtroEscolaridade = "Tudo")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Iniciando CarregarGraficoEditaisRendimentoAsync - Filtro: {filtroEscolaridade}");

                // Buscar editais com RendimentoProva calculado, ordenados por data da prova
                var query = _context.Editais
                    .Include(e => e.Escolaridade)
                    .Where(e => e.AcertosProva.HasValue && e.ErrosProva.HasValue); // Apenas editais com dados de prova

                // Aplicar filtro de escolaridade
                if (filtroEscolaridade != "Tudo")
                {
                    query = query.Where(e => e.Escolaridade != null && e.Escolaridade.Nome == filtroEscolaridade);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Filtro de escolaridade aplicado: {filtroEscolaridade}");
                }

                var editais = await query
                    .OrderBy(e => e.DataProvaTicks)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Editais encontrados (filtrados): {editais.Count}");

                if (editais.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Nenhum edital com dados de prova encontrado para o filtro selecionado");
                    PlotModelEditais = null;
                    DadosEditaisGrafico = new List<Edital>();
                    return;
                }

                // Preparar dados para o gráfico com debug detalhado
                var rendimentos = new List<double>();
                var categorias = new List<string>();
                var dadosEditais = new List<Edital>(); // Armazenar dados completos dos editais
                decimal maxRendimento = 0m;

                foreach (var edital in editais)
                {
                    var acertos = edital.AcertosProva ?? 0;
                    var erros = edital.ErrosProva ?? 0;
                    var total = acertos + erros;
                    var rendimento = edital.RendimentoProva ?? 0m;
                    var rendimentoDouble = (double)rendimento;

                    // Simular o cálculo manual para comparar
                    decimal calculoManual = total > 0 ? Math.Round((decimal)acertos / total * 100, 2) : 0m;

                    if (rendimento > maxRendimento)
                        maxRendimento = rendimento;

                    System.Diagnostics.Debug.WriteLine(
                        $"[DEBUG] {edital.Orgao} | A={acertos} E={erros} Total={total} | " +
                        $"RendimentoProva={rendimento:F2} | Cálculo Manual={calculoManual:F2} | " +
                        $"Double={rendimentoDouble:F2}");

                    rendimentos.Add(rendimentoDouble);
                    categorias.Add($"{edital.Orgao} - {new DateTime(edital.DataProvaTicks).Year}");
                    dadosEditais.Add(edital); // Armazenar edital completo
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Máximo RendimentoProva: {maxRendimento:F2}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Categorias: {string.Join(", ", categorias)}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Rendimentos: {string.Join(", ", rendimentos.Select(r => r.ToString("F2")))}");

                // Configurar gráfico com dados completos dos editais
                ConfigurarGraficoEditaisRendimento(categorias, rendimentos, dadosEditais);

                System.Diagnostics.Debug.WriteLine("[DEBUG] Gráfico de editais configurado com sucesso");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Erro CarregarGraficoEditaisRendimentoAsync]: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Configura o gráfico OxyPlot para exibir RendimentoProva dos editais por órgão
        /// </summary>
        private void ConfigurarGraficoEditaisRendimento(List<string> categorias, List<double> rendimentos, List<Edital> dadosEditais)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG ConfigurarGraficoEditaisRendimento] Entrando com {categorias.Count} categorias e {rendimentos.Count} rendimentos");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Rendimentos recebidos: {string.Join(", ", rendimentos.Select(r => r.ToString("F2")))}");

                if (categorias?.Count == 0 || rendimentos?.Count == 0)
                    return;

                // Armazenar dados dos editais para uso no tooltip
                DadosEditaisGrafico = dadosEditais;

                var plotModel = new PlotModel
                {
                    Background = OxyColors.Transparent,
                    PlotAreaBorderThickness = new OxyThickness(0),
                    PlotMargins = new OxyThickness(0, 10, 0, 30)
                };

                // ---------------------------------------------------------
                // Eixo X (CATEGORIAS - Órgão e Ano)
                // ---------------------------------------------------------
                var xAxis = new CategoryAxis
                {
                    Position = AxisPosition.Bottom,
                    IsTickCentered = true, // Centraliza os ticks nas categorias

                    AxislineStyle = LineStyle.Solid,
                    AxislineColor = OxyColor.FromRgb(150, 150, 150),
                    TickStyle = TickStyle.None,

                    MajorGridlineStyle = LineStyle.None,
                    MinorGridlineStyle = LineStyle.None,

                    TextColor = OxyColor.FromRgb(150, 150, 150),
                    FontSize = 10,

                    GapWidth = 0.1
                };

                // Adicionar categorias ao eixo
                foreach (var categoria in categorias)
                {
                    xAxis.Labels.Add(categoria);
                }

                plotModel.Axes.Add(xAxis);

                // ---------------------------------------------------------
                // Eixo Y (RENDIMENTO %) - REMOVIDO PARA INTERFACE MAIS LIMPA
                // ---------------------------------------------------------
                // Adicionar eixo Y invisível para desabilitar grid automático
                var yAxisRendimento = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Minimum = 0,
                    Maximum = 100,

                    // Remover todas as visualizações
                    AxislineStyle = LineStyle.None,
                    TickStyle = TickStyle.None,
                    MajorGridlineStyle = LineStyle.None,
                    MinorGridlineStyle = LineStyle.None,
                    IsAxisVisible = false  // Eixo completamente invisível
                };
                plotModel.Axes.Add(yAxisRendimento);

                // ---------------------------------------------------------
                // Série de Linhas - RendimentoProva
                // ---------------------------------------------------------
                var lineSeries = new LineSeries
                {
                    Title = "Rendimento da Prova",
                    Color = OxyColor.FromRgb(100, 181, 246), // Azul claro
                    StrokeThickness = 3,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 6,
                    MarkerStroke = OxyColor.FromRgb(100, 181, 246),
                    MarkerFill = OxyColors.White,
                    MarkerStrokeThickness = 2,

                    // Configurar o tooltip/tracker com formato simples
                    // Será melhorado com evento customizado
                    TrackerFormatString = "{0}\n{1}\nRendimento: {4:F2}%",

                    CanTrackerInterpolatePoints = false,

                    // Labels de dados sobre os pontos
                    LabelFormatString = "{1:F1}%",      // Formato: "XX.X%"
                    LabelMargin = 10,                   // Margem do ponto para o texto

                    TextColor = OxyColor.FromRgb(100, 181, 246),
                    FontSize = 11
                };

                // Adicionar pontos de dados
                // Com CategoryAxis, o X é a posição da categoria (0, 1, 2, ...)
                for (int i = 0; i < rendimentos.Count; i++)
                {
                    lineSeries.Points.Add(new DataPoint(i, rendimentos[i]));

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Ponto {i}: Categoria={categorias[i]}, Rendimento={rendimentos[i]:F2}%");
                }

                plotModel.Series.Add(lineSeries);

                // Atribuição Final
                PlotModelEditais = plotModel;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] PlotModelEditais atualizado com {lineSeries.Points.Count} pontos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Erro ConfigurarGraficoEditaisRendimento]: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task CarregarGraficoAcertosErrosAsync()
        {
            try
            {
                // Carregar estudos aplicando os filtros ativos
                var query = ConstruirQueryComFiltros();
                var todosEstudos = await query.ToListAsync();

                AcertosGrafico = todosEstudos.Sum(e => e.Acertos);
                ErrosGrafico = todosEstudos.Sum(e => e.Erros);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Gráfico Acertos/Erros carregado (COM FILTROS): Acertos={AcertosGrafico}, Erros={ErrosGrafico}, Total={todosEstudos.Count} estudos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Erro CarregarGraficoAcertosErrosAsync]: {ex.Message}");
            }
        }

        /// <summary>
        /// Manipulador para quando uma revisão é atualizada
        /// Recarrega a lista de próximas revisões para refletir mudanças em tempo real
        /// </summary>
        private void OnRevisaoAtualizada(object? sender, RevisaoEventArgs e)
        {
            if (e.Revisao == null) return;

            System.Diagnostics.Debug.WriteLine($"[DEBUG] HomeViewModel.OnRevisaoAtualizada - Revisão {e.Revisao.Id} foi atualizada");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] EstudoRealizadoId: {e.Revisao.EstudoRealizadoId}");

            // Recarregar a lista de próximas revisões na thread da UI
            System.Windows.Application.Current?.Dispatcher?.Invoke(async () =>
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Recarregando próximas revisões...");
                await CarregarProximasRevisoesAsync();
            });
        }
        protected override void OnRefresh()
        {
            _ = CarregarDadosAsync();
        }
    }
}
