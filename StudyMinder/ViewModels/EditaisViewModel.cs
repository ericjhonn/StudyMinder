using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Navigation;
using StudyMinder.Views;
using StudyMinder.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace StudyMinder.ViewModels
{
    public partial class EditaisViewModel : BaseViewModel
    {
        private readonly int _pageSize = 25;
        private int _currentPage = 1;
        private string _searchText = string.Empty;
        private bool _isCarregando = true;
        private bool _isFiltrosPanelVisible = false;
        private readonly EditalService _editalService;
        private readonly EditalTransactionService _transactionService;
        private readonly NavigationService _navigationService;
        private readonly RevisaoNotificacaoService _revisaoNotificacaoService;
        private readonly INotificationService _notificationService;
        private readonly IConfigurationService _configurationService;
        private readonly System.Timers.Timer _searchTimer;
        private readonly SemaphoreSlim _carregandoSemaphore = new(1, 1);

        /// <summary>
        /// Cache de editais para evitar recarregamentos desnecessários
        /// </summary>
        private List<Edital>? _cacheEditais;

        // Filtros
        private DateTime? _dataAberturaInicio;
        private DateTime? _dataAberturaFim;
        private DateTime? _dataProvaInicio;
        private DateTime? _dataProvaFim;
        private FaseEdital? _faseEditalFiltro;
        private bool _filtrarApenasNaoArquivados = true;

        public EditaisViewModel(EditalService editalService, EditalTransactionService transactionService, NavigationService navigationService, RevisaoNotificacaoService revisaoNotificacaoService, INotificationService notificationService, IConfigurationService configurationService)
        {
            Title = "Editais";
            _editalService = editalService;
            _transactionService = transactionService;
            _navigationService = navigationService;
            _revisaoNotificacaoService = revisaoNotificacaoService;
            _notificationService = notificationService;
            _configurationService = configurationService;
            
            // Timer para debounce da pesquisa
            _searchTimer = new System.Timers.Timer(300); // 300ms de delay
            _searchTimer.Elapsed += SearchTimer_Elapsed;
            _searchTimer.AutoReset = false;
            
            Editais = new ObservableCollection<Edital>();
            EditaisView = CollectionViewSource.GetDefaultView(Editais);
            EditaisView.Filter = FilterEditais;
            
            FasesEdital = new ObservableCollection<FaseEdital>();
            
            FirstPageCommand = new AsyncRelayCommand(FirstPageAsync, CanGoToFirstPage);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, CanGoToPreviousPage);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, CanGoToNextPage);
            LastPageCommand = new AsyncRelayCommand(LastPageAsync, CanGoToLastPage);

            LoadEditaisCommand = new AsyncRelayCommand(LoadEditaisAsync);
            AdicionarEditalCommand = new RelayCommand(AdicionarEdital);
            EditarEditalCommand = new RelayCommand<Edital>(EditarEdital);
            ExcluirEditalCommand = new AsyncRelayCommand<Edital>(ExcluirEditalAsync);
            DuplicarEditalCommand = new AsyncRelayCommand<Edital>(DuplicarEditalAsync);
            
            ToggleFiltrosPanelCommand = new RelayCommand(ToggleFiltrosPanel);
            AplicarFiltrosCommand = new AsyncRelayCommand(AplicarFiltrosAsync);
            LimparFiltrosCommand = new AsyncRelayCommand(LimparFiltrosAsync);
            
            // Inscrever nos eventos de notificação
            _revisaoNotificacaoService.RevisaoAdicionada += OnRevisaoAdicionada;
            _revisaoNotificacaoService.RevisaoAtualizada += OnRevisaoAtualizada;
            _revisaoNotificacaoService.RevisaoRemovida += OnRevisaoRemovida;
            
            // Carregar dados (filtros e lista principal) em segundo plano para não bloquear a UI
            _ = Task.Run(LoadFasesEditalAsync);
            _ = Task.Run(CarregarDadosIniciaisAsync);
        }

        private async Task CarregarDadosIniciaisAsync()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsCarregando = true);
                await LoadPageAsync();
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar editais: {ex.Message}");
                });
            }
            finally
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsCarregando = false);
            }
        }

        protected override void OnRefresh()
        {
            _ = LoadPageAsync();
        }

        public IAsyncRelayCommand LoadEditaisCommand { get; }
        public IRelayCommand AdicionarEditalCommand { get; }
        public IRelayCommand<Edital> EditarEditalCommand { get; }
        public IAsyncRelayCommand<Edital> ExcluirEditalCommand { get; }
        public IAsyncRelayCommand<Edital> DuplicarEditalCommand { get; }
        public IRelayCommand ToggleFiltrosPanelCommand { get; }
        public IAsyncRelayCommand AplicarFiltrosCommand { get; }
        public IAsyncRelayCommand LimparFiltrosCommand { get; }

        public ObservableCollection<Edital> Editais { get; }
        public ICollectionView EditaisView { get; }
        public ObservableCollection<FaseEdital> FasesEdital { get; }
        
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    UpdatePagination();
                }
            }
        }
        
        public int TotalPages { get; private set; }
        public int TotalCount { get; private set; }
        public int FilteredCount { get; private set; }
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    CurrentPage = 1;
                    _searchTimer.Stop();
                    _searchTimer.Start();
                }
            }
        }
        
        public bool IsCarregando
        {
            get => _isCarregando;
            set => SetProperty(ref _isCarregando, value);
        }
        
        public bool IsFiltrosPanelVisible
        {
            get => _isFiltrosPanelVisible;
            set => SetProperty(ref _isFiltrosPanelVisible, value);
        }
        
        // Propriedades de Filtro
        public DateTime? DataAberturaInicio
        {
            get => _dataAberturaInicio;
            set => SetProperty(ref _dataAberturaInicio, value);
        }
        
        public DateTime? DataAberturaFim
        {
            get => _dataAberturaFim;
            set => SetProperty(ref _dataAberturaFim, value);
        }
        
        public DateTime? DataProvaInicio
        {
            get => _dataProvaInicio;
            set => SetProperty(ref _dataProvaInicio, value);
        }
        
        public DateTime? DataProvaFim
        {
            get => _dataProvaFim;
            set => SetProperty(ref _dataProvaFim, value);
        }
        
        public FaseEdital? FaseEditalFiltro
        {
            get => _faseEditalFiltro;
            set => SetProperty(ref _faseEditalFiltro, value);
        }
        
        public bool FiltrarApenasNaoArquivados
        {
            get => _filtrarApenasNaoArquivados;
            set => SetProperty(ref _filtrarApenasNaoArquivados, value);
        }
        
        public IAsyncRelayCommand FirstPageCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand LastPageCommand { get; }
        
        private bool FilterEditais(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (item is not Edital edital) return false;

            // Busca case-insensitive e sem acentos/caracteres especiais
            return StringNormalizationHelper.ContainsIgnoreCaseAndAccents(edital.Cargo, SearchText) ||
                   StringNormalizationHelper.ContainsIgnoreCaseAndAccents(edital.Orgao, SearchText) ||
                   (edital.Banca != null && StringNormalizationHelper.ContainsIgnoreCaseAndAccents(edital.Banca, SearchText)) ||
                   (edital.Area != null && StringNormalizationHelper.ContainsIgnoreCaseAndAccents(edital.Area, SearchText));
        }
        
        private void UpdatePagination()
        {
            TotalPages = (int)Math.Ceiling((double)FilteredCount / _pageSize);
            OnPropertyChanged(nameof(TotalPages));
            (FirstPageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (PreviousPageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (NextPageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (LastPageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
        
        private async Task FirstPageAsync()
        {
            CurrentPage = 1;
            await LoadPageAsync();
        }
        
        private bool CanGoToFirstPage() => CurrentPage > 1;
        
        private async Task PreviousPageAsync()
        {
            CurrentPage--;
            await LoadPageAsync();
        }
        
        private bool CanGoToPreviousPage() => CurrentPage > 1;
        
        private async Task NextPageAsync()
        {
            CurrentPage++;
            await LoadPageAsync();
        }
        
        private bool CanGoToNextPage() => CurrentPage < TotalPages;
        
        private async Task LastPageAsync()
        {
            CurrentPage = TotalPages;
            await LoadPageAsync();
        }
        
        private bool CanGoToLastPage() => CurrentPage < TotalPages;
        
        private async Task LoadEditaisAsync()
        {
            await LoadPageAsync();
        }
        
        private async Task LoadPageAsync()
        {
            await _carregandoSemaphore.WaitAsync();
            try
            {
                // Obter configuração de arquivamento
                var settings = await _configurationService.LoadSettingsAsync();
                bool incluirArquivados = !settings.Archiving.HideInactiveEditals || FiltrarApenasNaoArquivados == false;
                
                var pagedResult = await _editalService.ObterPaginadoAsync(
                    CurrentPage,
                    _pageSize,
                    SearchText,
                    incluirArquivados,
                    FaseEditalFiltro?.Id);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Editais.Clear();
                    foreach (var edital in pagedResult.Items)
                    {
                        // Aplicar filtros de data
                        if (DataAberturaInicio.HasValue && edital.DataAbertura < DataAberturaInicio.Value)
                            continue;
                        if (DataAberturaFim.HasValue && edital.DataAbertura > DataAberturaFim.Value)
                            continue;
                        if (DataProvaInicio.HasValue && edital.DataProva < DataProvaInicio.Value)
                            continue;
                        if (DataProvaFim.HasValue && edital.DataProva > DataProvaFim.Value)
                            continue;

                        Editais.Add(edital);
                    }

                    TotalCount = pagedResult.TotalCount;
                    FilteredCount = Editais.Count;
                    UpdatePagination();
                    OnPropertyChanged(nameof(TotalCount));
                    OnPropertyChanged(nameof(FilteredCount));
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar editais: {ex.Message}");
                });
            }
            finally
            {
                _carregandoSemaphore.Release();
            }
        }
        
        private Data.StudyMinderContext CreateDbContext()
        {
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            var dbPath = Path.Combine(exeDir, "StudyMinder.db");
            if (!File.Exists(dbPath))
            {
                dbPath = Path.Combine(Directory.GetParent(exeDir)?.FullName ?? string.Empty, "StudyMinder.db");
            }

            var optionsBuilder = new DbContextOptionsBuilder<Data.StudyMinderContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            
            return new Data.StudyMinderContext(optionsBuilder.Options);
        }
        
        private async Task LoadFasesEditalAsync()
        {
            await _carregandoSemaphore.WaitAsync();
            try
            {
                // Carregar fases de editais diretamente do banco
                using var context = CreateDbContext();
                var fases = await context.FasesEdital.ToListAsync();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    FasesEdital.Clear();
                    FasesEdital.Add(new FaseEdital { Id = 0, Fase = "Todos" });
                    foreach (var fase in fases)
                    {
                        FasesEdital.Add(fase);
                    }
                });
            }
            catch
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Se houver erro, apenas adicionar opção "Todos"
                    FasesEdital.Clear();
                    FasesEdital.Add(new FaseEdital { Id = 0, Fase = "Todos" });
                });
            }
            finally
            {
                _carregandoSemaphore.Release();
            }
        }

        private void AdicionarEdital()
        {
            _navigationService.NavigateTo<EditarEditalViewModel>();
        }

        private void EditarEdital(Edital? edital)
        {
            if (edital == null) return;
            _navigationService.NavigateTo<EditarEditalViewModel>(edital);
        }

        private async Task ExcluirEditalAsync(Edital? edital)
        {
            if (edital == null) return;

            var result = _notificationService.ShowConfirmation("Confirmar Exclusão", $"Tem certeza que deseja excluir o edital '{edital.Cargo} - {edital.Orgao}'?");
            if (result != ToastMessageBoxResult.Yes) return;

            try
            {
                await _editalService.ExcluirAsync(edital.Id);
                _notificationService.ShowSuccess("Excluído", "Edital excluído com sucesso.");
                await LoadPageAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Excluir", $"Erro ao excluir o edital: {ex.Message}");
            }
        }

        private async Task DuplicarEditalAsync(Edital? edital)
        {
            if (edital == null) return;

            var result = _notificationService.ShowConfirmation(
                "Confirmar Duplicação",
                $"Deseja duplicar o edital '{edital.Cargo} - {edital.Orgao}'?\n\nTodos os assuntos e eventos do cronograma serão duplicados.");
            
            if (result != ToastMessageBoxResult.Yes) return;

            try
            {
                IsCarregando = true;

                // Obter o edital completo com assuntos e cronogramas
                var editalCompleto = await _editalService.ObterPorIdAsync(edital.Id);
                if (editalCompleto == null)
                {
                    _notificationService.ShowError("Erro", "Não foi possível duplicar o edital.");
                    return;
                }

                // Obter assuntos do edital
                var assuntos = await _editalService.ObterAssuntosDoEditalAsync(editalCompleto.Id);
                var assuntosIds = assuntos.Select(a => a.Id).ToList();

                // Criar novo edital com os dados duplicados
                var novoEdital = new Edital
                {
                    Cargo = $"{editalCompleto.Cargo} Cópia",
                    Orgao = editalCompleto.Orgao,
                    Salario = editalCompleto.Salario,
                    VagasImediatas = editalCompleto.VagasImediatas,
                    VagasCadastroReserva = editalCompleto.VagasCadastroReserva,
                    Concorrencia = editalCompleto.Concorrencia,
                    Colocacao = null, // Resetar informações de prova no edital duplicado
                    NumeroInscricao = editalCompleto.NumeroInscricao,
                    AcertosProva = null,
                    ErrosProva = null,
                    BrancosProva = null,
                    AnuladasProva = null,
                    TipoProvaId = editalCompleto.TipoProvaId,
                    EscolaridadeId = editalCompleto.EscolaridadeId,
                    Banca = editalCompleto.Banca,
                    Area = editalCompleto.Area,
                    Link = editalCompleto.Link,
                    ValorInscricao = editalCompleto.ValorInscricao,
                    FaseEditalId = editalCompleto.FaseEditalId,
                    ProvaDiscursiva = editalCompleto.ProvaDiscursiva,
                    ProvaTitulos = editalCompleto.ProvaTitulos,
                    ProvaTaf = editalCompleto.ProvaTaf,
                    ProvaPratica = editalCompleto.ProvaPratica,
                    DataAbertura = editalCompleto.DataAbertura,
                    DataProva = editalCompleto.DataProva,
                    Arquivado = false,
                    Validade = editalCompleto.Validade,
                    DataHomologacaoTicks = editalCompleto.DataHomologacaoTicks,
                    BoletoPago = editalCompleto.BoletoPago
                };

                // Duplicar cronogramas
                var novosCronogramas = new List<EditalCronograma>();
                if (editalCompleto.EditalCronogramas != null)
                {
                    foreach (var cronograma in editalCompleto.EditalCronogramas)
                    {
                        novosCronogramas.Add(new EditalCronograma
                        {
                            Evento = cronograma.Evento,
                            DataEventoTicks = cronograma.DataEventoTicks,
                            Concluido = false,
                            Ignorado = false
                        });
                    }
                }

                // Salvar o novo edital e seus relacionamentos
                await _editalService.SalvarEditalComRelacionamentosAsync(
                    novoEdital,
                    assuntosIds,
                    novosCronogramas);

                _notificationService.ShowSuccess(
                    "Edital Duplicado",
                    $"Edital '{novoEdital.Cargo}' duplicado com sucesso!");

                // Recarregar a lista
                await LoadPageAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError(
                    "Erro ao Duplicar",
                    $"Erro ao duplicar o edital: {ex.Message}");
            }
            finally
            {
                IsCarregando = false;
            }
        }
        
        private void ToggleFiltrosPanel()
        {
            IsFiltrosPanelVisible = !IsFiltrosPanelVisible;
        }
        
        private async Task AplicarFiltrosAsync()
        {
            CurrentPage = 1;
            await LoadPageAsync();
        }
        
        private async Task LimparFiltrosAsync()
        {
            DataAberturaInicio = null;
            DataAberturaFim = null;
            DataProvaInicio = null;
            DataProvaFim = null;
            FaseEditalFiltro = null;
            FiltrarApenasNaoArquivados = true;
            SearchText = string.Empty;
            
            CurrentPage = 1;
            await LoadPageAsync();
        }

        /// <summary>
        /// Manipulador para quando uma revisão é adicionada
        /// Invalida o cache de editais
        /// </summary>
        private void OnRevisaoAdicionada(object? sender, RevisaoEventArgs e)
        {
            _cacheEditais = null;
        }

        /// <summary>
        /// Manipulador para quando uma revisão é atualizada
        /// Invalida o cache de editais
        /// </summary>
        private void OnRevisaoAtualizada(object? sender, RevisaoEventArgs e)
        {
            _cacheEditais = null;
        }

        /// <summary>
        /// Manipulador para quando uma revisão é removida
        /// Invalida o cache de editais
        /// </summary>
        private void OnRevisaoRemovida(object? sender, RevisaoEventArgs e)
        {
            _cacheEditais = null;
        }

        /// <summary>
        /// Manipulador do timer de debounce para pesquisa
        /// Executa a pesquisa após 300ms de inatividade
        /// </summary>
        private void SearchTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Executando pesquisa para: '{SearchText}'");
            
            // Executar na thread da UI para evitar erro de CollectionView
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadPageAsync();
            });
        }
    }
}
