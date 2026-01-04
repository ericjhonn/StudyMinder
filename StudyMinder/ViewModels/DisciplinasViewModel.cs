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

namespace StudyMinder.ViewModels
{
    public partial class DisciplinasViewModel : BaseViewModel, IRefreshable
    {
        private readonly int _pageSize = 25;
        private int _currentPage = 1;
        private string _searchText = string.Empty;
        private bool _isCarregando = true;
        private readonly DisciplinaService _disciplinaService;
        private readonly AssuntoService _assuntoService;
        private readonly DisciplinaAssuntoTransactionService _transactionService;
        private readonly NavigationService _navigationService;
        private readonly INotificationService _notificationService;
        private readonly IConfigurationService _configurationService;
        private readonly System.Timers.Timer _searchTimer;
        private readonly SemaphoreSlim _carregandoSemaphore = new(1, 1);

        public DisciplinasViewModel(DisciplinaService disciplinaService, AssuntoService assuntoService, DisciplinaAssuntoTransactionService transactionService, NavigationService navigationService, INotificationService notificationService, IConfigurationService configurationService)
        {
            Title = "Disciplinas";
            _disciplinaService = disciplinaService;
            _assuntoService = assuntoService;
            _transactionService = transactionService;
            _navigationService = navigationService;
            _notificationService = notificationService;
            _configurationService = configurationService;
            
            // Timer para debounce da pesquisa
            _searchTimer = new System.Timers.Timer(300); // 300ms de delay
            _searchTimer.Elapsed += SearchTimer_Elapsed;
            _searchTimer.AutoReset = false;
            
            Disciplinas = new ObservableCollection<Disciplina>();
            DisciplinasView = CollectionViewSource.GetDefaultView(Disciplinas);
            DisciplinasView.Filter = FilterDisciplinas;
            
            FirstPageCommand = new AsyncRelayCommand(FirstPageAsync, CanGoToFirstPage);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, CanGoToPreviousPage);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, CanGoToNextPage);
            LastPageCommand = new AsyncRelayCommand(LastPageAsync, CanGoToLastPage);

            LoadDisciplinasCommand = new AsyncRelayCommand(LoadDisciplinasAsync);
            AdicionarDisciplinaCommand = new RelayCommand(AdicionarDisciplina);
            EditarDisciplinaCommand = new RelayCommand<Disciplina>(EditarDisciplina);
            ExcluirDisciplinaCommand = new AsyncRelayCommand<Disciplina>(ExcluirDisciplinaAsync);
            
            // ✅ Carregar dados automaticamente no construtor (como em EstudosViewModel)
            // Isso garante que IsCarregando seja definido ANTES da View ser renderizada
            _ = Task.Run(async () => await CarregarDadosIniciaisAsync());
        }

        protected override void OnRefresh()
        {
            _ = LoadPageAsync();
        }

        /// <summary>
        /// Carrega dados iniciais do ViewModel (chamado no construtor)
        /// Similar a EstudosViewModel.CarregarDadosIniciaisAsync()
        /// </summary>
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
                    _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar disciplinas: {ex.Message}");
                });
            }
            finally
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsCarregando = false);
            }
        }

        public IAsyncRelayCommand LoadDisciplinasCommand { get; }
        public IRelayCommand AdicionarDisciplinaCommand { get; }
        public IRelayCommand<Disciplina> EditarDisciplinaCommand { get; }
        public IAsyncRelayCommand<Disciplina> ExcluirDisciplinaCommand { get; }

        public ObservableCollection<Disciplina> Disciplinas { get; }
        
        public ICollectionView DisciplinasView { get; }
        
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
                    // Implementar pesquisa com debounce
                    CurrentPage = 1;
                    _searchTimer.Stop();
                    _searchTimer.Start();
                }
            }
        }
        
        public IAsyncRelayCommand FirstPageCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand LastPageCommand { get; }
        
        public bool IsCarregando
        {
            get => _isCarregando;
            set => SetProperty(ref _isCarregando, value);
        }
        
        private bool FilterDisciplinas(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (item is not Disciplina disciplina) return false;

            // Busca case-insensitive e sem acentos/caracteres especiais
            return StringNormalizationHelper.ContainsIgnoreCaseAndAccents(disciplina.Nome, SearchText);
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
        
        private async Task LoadDisciplinasAsync()
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
                bool incluirArquivadas = !settings.Archiving.HideArchivedDisciplines;
                
                var pagedResult = await _disciplinaService.ObterPaginadoAsync(CurrentPage, _pageSize, SearchText, incluirArquivadas);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Disciplinas.Clear();
                    foreach (var disciplina in pagedResult.Items)
                    {
                        Disciplinas.Add(disciplina);
                    }

                    TotalCount = pagedResult.TotalCount;
                    FilteredCount = pagedResult.TotalCount; // Neste caso, são o mesmo.
                    UpdatePagination();
                    OnPropertyChanged(nameof(TotalCount));
                    OnPropertyChanged(nameof(FilteredCount));
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar disciplinas: {ex.Message}");
                });
            }
            finally
            {
                _carregandoSemaphore.Release();
            }
        }

        private void AdicionarDisciplina()
        {
            var viewModel = new EditarDisciplinaViewModel(_disciplinaService, _assuntoService, _transactionService, _navigationService);
            var view = new ViewDisciplinaEditar { DataContext = viewModel };
            _navigationService.NavigateTo(view);
        }

        private void EditarDisciplina(Disciplina? disciplina)
        {
            if (disciplina == null) return;
            var viewModel = new EditarDisciplinaViewModel(_disciplinaService, _assuntoService, _transactionService, _navigationService, disciplina);
            var view = new ViewDisciplinaEditar { DataContext = viewModel };
            _navigationService.NavigateTo(view);
        }

        private async Task ExcluirDisciplinaAsync(Disciplina? disciplina)
        {
            if (disciplina == null) return;

            var result = _notificationService.ShowConfirmation("Confirmar Exclusão", $"Tem certeza que deseja excluir a disciplina '{disciplina.Nome}'?");
            if (result != ToastMessageBoxResult.Yes) return;

            try
            {
                if (disciplina.TotalAssuntos > 0)
                {
                    await _disciplinaService.ArquivarAsync(disciplina.Id);
                    _notificationService.ShowInfo("Arquivada", "A disciplina foi arquivada pois possui assuntos vinculados.");
                }
                else
                {
                    await _disciplinaService.ExcluirAsync(disciplina.Id);
                    _notificationService.ShowSuccess("Excluída", "Disciplina excluída com sucesso.");
                }
                await LoadPageAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Excluir", $"Erro ao excluir a disciplina: {ex.Message}");
            }
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

        /// <summary>
        /// Implementação de IRefreshable
        /// Recarrega os dados quando retornando de uma view de edição
        /// </summary>
        public void RefreshData()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] 🔄 DisciplinasViewModel.RefreshData() - Recarregando dados");
            _ = LoadPageAsync();
        }
    }
}
