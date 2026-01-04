using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Views;
using StudyMinder.Navigation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using System.Threading;

namespace StudyMinder.ViewModels
{
    public partial class EstudosViewModel : BaseViewModel, IDisposable
    {
        // Evento para notificar atualizações na lista de estudos
        public event EventHandler? EstudosAtualizados;
        private readonly EstudoService _estudoService;
        private readonly TipoEstudoService _tipoEstudoService;
        private readonly AssuntoService _assuntoService;
        private readonly DisciplinaService _disciplinaService;
        private readonly EstudoTransactionService _transactionService;
        private readonly PomodoroTimerService _pomodoroService;
        private readonly NavigationService _navigationService;
        private readonly EstudoNotificacaoService _estudoNotificacaoService;
        private readonly RevisaoService _revisaoService;
        private readonly IConfigurationService _configurationService;
        private readonly INotificationService _notificationService;
        private readonly SemaphoreSlim _carregandoSemaphore = new SemaphoreSlim(1, 1);
        private bool _limpandoFiltros = false;

        [ObservableProperty]
        private ObservableCollection<Estudo> estudos = new();

        [ObservableProperty]
        private ObservableCollection<TipoEstudo> tiposEstudo = new();

        [ObservableProperty]
        private ObservableCollection<Disciplina> disciplinas = new();

        [ObservableProperty]
        private ObservableCollection<Assunto> assuntos = new();

        [ObservableProperty]
        private Estudo? estudoSelecionado;

        [ObservableProperty]
        private bool isTimerAtivo;

        [ObservableProperty]
        private TimeSpan tempoEstudo = TimeSpan.Zero;

        [ObservableProperty]
        private string tempoEstudoTexto = "00:00:00";

        [ObservableProperty]
        private DateTime? dataFiltroInicio;

        [ObservableProperty]
        private DateTime? dataFiltroFim;

        [ObservableProperty]
        private Disciplina? disciplinaFiltro;

        [ObservableProperty]
        private Assunto? assuntoFiltro;

        [ObservableProperty]
        private TipoEstudo? tipoEstudoFiltro;

        [ObservableProperty]
        private bool filtrarAssuntosConcluidos = false;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private int totalPages = 1;

        [ObservableProperty]
        private int totalRegistros;

        [ObservableProperty]
        private int filteredCount;

        [ObservableProperty]
        private bool isCarregando;

        [ObservableProperty]
        private bool isFiltrosPanelVisible = false;

        [ObservableProperty]
        private bool filtrosAtivos = false;

        [ObservableProperty]
        private int contadorFiltrosAtivos = 0;

        // Estatísticas
        [ObservableProperty]
        private double totalHorasHoje;

        [ObservableProperty]
        private int totalQuestoesHoje;

        [ObservableProperty]
        private double rendimentoHoje;

        [ObservableProperty]
        private int totalEstudosHoje;

        private readonly System.Timers.Timer _timer;
        private readonly System.Timers.Timer _searchTimer;
        private DateTime _inicioEstudo;
        private const int ITENS_POR_PAGINA = 20;

        public ICollectionView EstudosView { get; private set; }

        public EstudosViewModel(
            EstudoService estudoService,
            TipoEstudoService tipoEstudoService,
            AssuntoService assuntoService,
            DisciplinaService disciplinaService,
            EstudoTransactionService transactionService,
            PomodoroTimerService pomodoroService,
            NavigationService navigationService,
            EstudoNotificacaoService estudoNotificacaoService,
            RevisaoService revisaoService,
            INotificationService notificationService,
            IConfigurationService configurationService = null!)
        {
            Title = "Registro de Estudos";
            
            _estudoService = estudoService;
            _tipoEstudoService = tipoEstudoService;
            _assuntoService = assuntoService;
            _disciplinaService = disciplinaService;
            _transactionService = transactionService;
            _pomodoroService = pomodoroService;
            _navigationService = navigationService;
            _estudoNotificacaoService = estudoNotificacaoService;
            _revisaoService = revisaoService;
            _notificationService = notificationService;
            _configurationService = configurationService ?? new ConfigurationService();
            
            // Inscrever-se nos eventos de notificação
            _estudoNotificacaoService.EstudoAdicionado += OnEstudoAdicionado;
            _estudoNotificacaoService.EstudoAtualizado += OnEstudoAtualizado;
            _estudoNotificacaoService.EstudoRemovido += OnEstudoRemovido;

            EstudosView = CollectionViewSource.GetDefaultView(Estudos);
            EstudosView.SortDescriptions.Add(new SortDescription(nameof(Estudo.Data), ListSortDirection.Descending));

            _timer = new System.Timers.Timer(1000); // Atualiza a cada segundo
            _timer.Elapsed += Timer_Elapsed;

            // Timer para debounce da pesquisa otimizado
            _searchTimer = new System.Timers.Timer(300); // 300ms de delay
            _searchTimer.Elapsed += SearchTimer_Elapsed;
            _searchTimer.AutoReset = false;

            FirstPageCommand = new AsyncRelayCommand(FirstPageAsync, CanGoToFirstPage);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, CanGoToPreviousPage);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, CanGoToNextPage);
            LastPageCommand = new AsyncRelayCommand(LastPageAsync, CanGoToLastPage);

            _ = Task.Run(async () => await CarregarDadosIniciaisAsync());
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsTimerAtivo)
            {
                TempoEstudo = DateTime.Now - _inicioEstudo;
                TempoEstudoTexto = TempoEstudo.ToString(@"hh\:mm\:ss");
            }
        }

        private async Task CarregarDadosIniciaisAsync()
        {
            try
            {
                IsCarregando = true;

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

                await CarregarEstudosAsync();
                await CarregarEstatisticasHojeAsync();
            }
            catch (Exception ex)
            {
                // Log error
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar dados: {ex.Message}");
            }
            finally
            {
                IsCarregando = false;
            }
        }

        [RelayCommand]
        private async Task CarregarEstudosAsync()
        {
            // Proteger contra operações concorrentes no DbContext
            await _carregandoSemaphore.WaitAsync();
            try
            {
                IsCarregando = true;

                PagedResult<Estudo> resultado;

                // Validar intervalo de datas
                if (DataFiltroInicio.HasValue && DataFiltroFim.HasValue && DataFiltroFim < DataFiltroInicio)
                {
                    _notificationService.ShowWarning("Aviso", "A data final não pode ser anterior à data inicial.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] CarregarEstudosAsync - TipoEstudoFiltro: {TipoEstudoFiltro?.Nome ?? "null"} (ID: {TipoEstudoFiltro?.Id})");
                
                // Se houver texto de pesquisa, usar o método de pesquisa
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Usando PesquisarAsync com SearchText: {SearchText}");
                    resultado = await _estudoService.PesquisarAsync(
                        SearchText,
                        CurrentPage,
                        ITENS_POR_PAGINA,
                        AssuntoFiltro?.Id,
                        DisciplinaFiltro?.Id,
                        DataFiltroInicio,
                        DataFiltroFim,
                        FiltrarAssuntosConcluidos,
                        TipoEstudoFiltro?.Id);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Usando ObterPaginadoAsync sem SearchText");
                    resultado = await _estudoService.ObterPaginadoAsync(
                        CurrentPage,
                        ITENS_POR_PAGINA,
                        AssuntoFiltro?.Id,
                        DisciplinaFiltro?.Id,
                        TipoEstudoFiltro?.Id,
                        DataFiltroInicio,
                        DataFiltroFim,
                        FiltrarAssuntosConcluidos);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Estudos.Clear();
                    foreach (var estudo in resultado.Items)
                        Estudos.Add(estudo);

                    TotalRegistros = resultado.TotalCount;
                    FilteredCount = resultado.TotalCount;
                    TotalPages = (int)Math.Ceiling((double)resultado.TotalCount / ITENS_POR_PAGINA);
                    NotifyPaginationCanExecuteChanged();
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar estudos: {ex.Message}");
            }
            finally
            {
                IsCarregando = false;
                _carregandoSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task CarregarEstatisticasHojeAsync()
        {
            try
            {
                var hoje = DateTime.Today;
                var amanha = hoje.AddDays(1);

                var resultado = await _estudoService.ObterPaginadoAsync(
                    1, int.MaxValue, null, null, TipoEstudoFiltro?.Id, hoje, amanha);

                var estudosHoje = resultado.Items;

                TotalHorasHoje = estudosHoje.Sum(e => e.Duracao.TotalHours);
                TotalQuestoesHoje = estudosHoje.Sum(e => e.TotalQuestoes);
                TotalEstudosHoje = estudosHoje.Count();

                var totalAcertos = estudosHoje.Sum(e => e.Acertos);
                var totalQuestoes = estudosHoje.Sum(e => e.TotalQuestoes);
                RendimentoHoje = totalQuestoes > 0 ? Math.Round((double)totalAcertos / totalQuestoes * 100, 1) : 0;
            }
            catch (Exception)
            {
                // Log error silently for statistics
            }
        }

        [RelayCommand]
        private void IniciarTimer()
        {
            if (!IsTimerAtivo)
            {
                _inicioEstudo = DateTime.Now;
                TempoEstudo = TimeSpan.Zero;
                IsTimerAtivo = true;
                _timer.Start();
            }
        }

        [RelayCommand]
        private void PausarTimer()
        {
            if (IsTimerAtivo)
            {
                IsTimerAtivo = false;
                _timer.Stop();
            }
        }

        [RelayCommand]
        private void ReiniciarTimer()
        {
            _timer.Stop();
            IsTimerAtivo = false;
            TempoEstudo = TimeSpan.Zero;
            TempoEstudoTexto = "00:00:00";
        }
        
        [RelayCommand]
        private void AdicionarEstudo()
        {
            var viewModel = new EditarEstudoViewModel(_estudoService, _tipoEstudoService, _assuntoService, _disciplinaService, _transactionService, _navigationService, _revisaoService, _notificationService, _configurationService);
            viewModel.EstudoSalvo += async (s, e) => 
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await CarregarEstudosAsync();
                    await CarregarEstatisticasHojeAsync();
                    EstudosAtualizados?.Invoke(this, EventArgs.Empty);
                });
            };
            var view = new ViewEstudoEditar { DataContext = viewModel };
            _navigationService.NavigateTo(view);
        }

        [RelayCommand]
        private void EditarEstudo(Estudo? estudo)
        {
            if (estudo == null) return;

            var viewModel = new EditarEstudoViewModel(_estudoService, _tipoEstudoService, _assuntoService, _disciplinaService, _transactionService, _navigationService, _revisaoService, _notificationService, _configurationService, estudo);
            viewModel.EstudoSalvo += async (s, e) => 
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await CarregarEstudosAsync();
                    await CarregarEstatisticasHojeAsync();
                    EstudosAtualizados?.Invoke(this, EventArgs.Empty);
                });
            };
            var view = new ViewEstudoEditar { DataContext = viewModel };
            _navigationService.NavigateTo(view);
        }

        [RelayCommand]
        private async Task ExcluirEstudo(Estudo? estudo)
        {
            if (estudo == null) return;

            System.Diagnostics.Debug.WriteLine($"[EstudosViewModel] ExcluirEstudo chamado para: {estudo.Assunto?.Nome}");

            var resultado = _notificationService.ShowConfirmation(
                "Confirmar Exclusão",
                $"Deseja realmente excluir o estudo de {estudo.Assunto?.Nome}?");

            System.Diagnostics.Debug.WriteLine($"[EstudosViewModel] Resultado da confirmação: {resultado}");

            if (resultado == ToastMessageBoxResult.Yes)
            {
                System.Diagnostics.Debug.WriteLine($"[EstudosViewModel] Usuário confirmou exclusão");
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[EstudosViewModel] Chamando ExcluirAsync para ID: {estudo.Id}");
                    await _estudoService.ExcluirAsync(estudo.Id);
                    System.Diagnostics.Debug.WriteLine($"[EstudosViewModel] Estudo excluído com sucesso");
                    
                    await CarregarEstudosAsync();
                    await CarregarEstatisticasHojeAsync();
                    EstudosAtualizados?.Invoke(this, EventArgs.Empty);
                    _notificationService.ShowSuccess("Sucesso", "Estudo excluído com sucesso!");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EstudosViewModel] Erro ao excluir: {ex.Message}");
                    _notificationService.ShowError("Erro ao Excluir", $"Erro ao excluir estudo: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[EstudosViewModel] Usuário cancelou exclusão");
            }
        }

        private void AtualizarIndicadorFiltros()
        {
            // Contar quantos filtros estão ativos
            int contador = 0;
            if (!string.IsNullOrWhiteSpace(SearchText)) contador++;
            if (DataFiltroInicio.HasValue) contador++;
            if (DataFiltroFim.HasValue) contador++;
            if (DisciplinaFiltro != null) contador++;
            if (AssuntoFiltro != null) contador++;
            if (TipoEstudoFiltro != null) contador++;
            if (FiltrarAssuntosConcluidos) contador++;

            ContadorFiltrosAtivos = contador;
            FiltrosAtivos = contador > 0;
        }

        [RelayCommand]
        private async Task AplicarFiltros()
        {
            AtualizarIndicadorFiltros();
            CurrentPage = 1;
            await CarregarEstudosAsync();
            
            // Mostrar feedback com resultado
            if (FilteredCount > 0)
            {
                _notificationService.ShowSuccess(
                    "Filtros Aplicados",
                    $"{FilteredCount} estudo(s) encontrado(s) com os filtros aplicados.");
            }
            else
            {
                _notificationService.ShowInfo(
                    "Nenhum Resultado",
                    "Nenhum estudo encontrado com os filtros aplicados.");
            }
        }

        [RelayCommand]
        private async Task LimparFiltros()
        {
            // Aguardar se houver operação em andamento
            if (IsCarregando)
            {
                await Task.Delay(100);
            }

            // Marcar que estamos limpando filtros para evitar chamadas automáticas
            _limpandoFiltros = true;
            
            try
            {
                // Parar o timer de pesquisa para evitar chamadas automáticas
                _searchTimer.Stop();
                
                SearchText = string.Empty;
                DataFiltroInicio = null;
                DataFiltroFim = null;
                DisciplinaFiltro = null;
                AssuntoFiltro = null;
                TipoEstudoFiltro = null;
                FiltrarAssuntosConcluidos = false;
                AtualizarIndicadorFiltros();
                CurrentPage = 1;
                await CarregarEstudosAsync();
            }
            finally
            {
                _limpandoFiltros = false;
                // Reiniciar o timer de pesquisa
                _searchTimer.Start();
            }
        }

        private bool CanGoToFirstPage() => CurrentPage > 1 && TotalPages > 0;
        private bool CanGoToPreviousPage() => CurrentPage > 1 && TotalPages > 0;
        private bool CanGoToNextPage() => CurrentPage < TotalPages && TotalPages > 0;
        private bool CanGoToLastPage() => CurrentPage < TotalPages && TotalPages > 0;

        public IAsyncRelayCommand FirstPageCommand { get; private set; }
        public IAsyncRelayCommand PreviousPageCommand { get; private set; }
        public IAsyncRelayCommand NextPageCommand { get; private set; }
        public IAsyncRelayCommand LastPageCommand { get; private set; }

        private async Task FirstPageAsync()
        {
            if (CanGoToFirstPage())
            {
                CurrentPage = 1;
                await CarregarEstudosAsync();
                NotifyPaginationCanExecuteChanged();
            }
        }

        private async Task PreviousPageAsync()
        {
            if (CanGoToPreviousPage())
            {
                CurrentPage--;
                await CarregarEstudosAsync();
                NotifyPaginationCanExecuteChanged();
            }
        }

        private async Task NextPageAsync()
        {
            if (CanGoToNextPage())
            {
                CurrentPage++;
                await CarregarEstudosAsync();
                NotifyPaginationCanExecuteChanged();
            }
        }

        private async Task LastPageAsync()
        {
            if (CanGoToLastPage())
            {
                CurrentPage = TotalPages;
                await CarregarEstudosAsync();
                NotifyPaginationCanExecuteChanged();
            }
        }

        private void NotifyPaginationCanExecuteChanged()
        {
            (FirstPageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (PreviousPageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (NextPageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (LastPageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void ToggleFiltrosPanel()
        {
            IsFiltrosPanelVisible = !IsFiltrosPanelVisible;
        }

        partial void OnSearchTextChanged(string value)
        {
            // Implementar pesquisa com debounce
            CurrentPage = 1; // Resetar para primeira página ao pesquisar
            NotifyPaginationCanExecuteChanged();
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private async void SearchTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // Não executar pesquisa se estamos limpando filtros
            if (_limpandoFiltros)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Ignorando pesquisa - limpando filtros");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"Executando pesquisa para: '{SearchText}'");
            await CarregarEstudosAsync();
        }

        partial void OnDisciplinaFiltroChanged(Disciplina? value)
        {
            // Não processar se estamos limpando filtros
            if (_limpandoFiltros)
                return;

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



        private void OnEstudoAdicionado(object? sender, EstudoEventArgs e)
        {
            // Recarregar os estudos e estatísticas quando um novo estudo é adicionado
            _ = Task.Run(async () =>
            {
                await CarregarEstudosAsync();
                await CarregarEstatisticasHojeAsync();
                EstudosAtualizados?.Invoke(this, EventArgs.Empty);
            });
        }

        private void OnEstudoAtualizado(object? sender, EstudoEventArgs e)
        {
            // Recarregar os estudos e estatísticas quando um estudo é atualizado
            _ = Task.Run(async () =>
            {
                await CarregarEstudosAsync();
                await CarregarEstatisticasHojeAsync();
                EstudosAtualizados?.Invoke(this, EventArgs.Empty);
            });
        }

        private void OnEstudoRemovido(object? sender, EstudoEventArgs e)
        {
            // Recarregar os estudos e estatísticas quando um estudo é removido
            _ = Task.Run(async () =>
            {
                await CarregarEstudosAsync();
                await CarregarEstatisticasHojeAsync();
                EstudosAtualizados?.Invoke(this, EventArgs.Empty);
            });
        }

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _timer?.Dispose();
                    _searchTimer?.Dispose();
                    _carregandoSemaphore?.Dispose();
                    
                    // Desinscrever-se dos eventos
                    _estudoNotificacaoService.EstudoAdicionado -= OnEstudoAdicionado;
                    _estudoNotificacaoService.EstudoAtualizado -= OnEstudoAtualizado;
                    _estudoNotificacaoService.EstudoRemovido -= OnEstudoRemovido;
                }
                _disposed = true;
            }
        }
    }
}
