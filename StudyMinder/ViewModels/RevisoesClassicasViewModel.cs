using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Navigation;
using StudyMinder.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

namespace StudyMinder.ViewModels
{
    public partial class RevisoesClassicasViewModel : BaseViewModel
    {
        private readonly RevisaoService _revisaoService;
        private readonly EstudoService _estudoService;
        private readonly NavigationService _navigationService;
        private readonly TipoEstudoService _tipoEstudoService;
        private readonly AssuntoService _assuntoService;
        private readonly DisciplinaService _disciplinaService;
        private readonly EstudoTransactionService _transactionService;
        private readonly RevisaoNotificacaoService _revisaoNotificacaoService;
        private readonly INotificationService _notificationService;
        private readonly IConfigurationService _configurationService;
        private bool _emModoRevisao = false;
        private readonly System.Timers.Timer _searchTimer;

        /// <summary>
        /// Cache de revisões para evitar recarregamentos desnecessários
        /// </summary>
        private List<Revisao>? _cacheRevisoes;

        [ObservableProperty]
        private ObservableCollection<Revisao> _revisoesPendentes = new();

        [ObservableProperty]
        private Revisao? _selectedItem;

        [ObservableProperty]
        private string _searchText = string.Empty;

        partial void OnSearchTextChanged(string value)
        {
            // Implementar pesquisa com debounce
            PaginaAtual = 1;
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        [ObservableProperty]
        private int _paginaAtual = 1;

        [ObservableProperty]
        private int _totalPaginas = 1;

        [ObservableProperty]
        private int _totalItens = 0;

        [ObservableProperty]
        private int _itensPorPagina = 20;

        [ObservableProperty]
        private bool _carregando = false;

        [ObservableProperty]
        private bool _isCarregando = false;

        [ObservableProperty]
        private int _filteredCount = 0;

        // Propriedades alias para compatibilidade com ViewEstudos
        public int CurrentPage => PaginaAtual;
        public int TotalPages => TotalPaginas;

        public RevisoesClassicasViewModel(
            RevisaoService revisaoService,
            EstudoService estudoService,
            NavigationService navigationService,
            TipoEstudoService tipoEstudoService,
            AssuntoService assuntoService,
            DisciplinaService disciplinaService,
            EstudoTransactionService transactionService,
            RevisaoNotificacaoService revisaoNotificacaoService,
            INotificationService notificationService,
            IConfigurationService configurationService = null!)
        {
            _revisaoService = revisaoService;
            _estudoService = estudoService;
            _navigationService = navigationService;
            _tipoEstudoService = tipoEstudoService;
            _assuntoService = assuntoService;
            _disciplinaService = disciplinaService;
            _transactionService = transactionService;
            _revisaoNotificacaoService = revisaoNotificacaoService;
            _notificationService = notificationService;
            _configurationService = configurationService ?? new ConfigurationService();
            Title = "Revisões Clássicas";

            // Timer para debounce da pesquisa
            _searchTimer = new System.Timers.Timer(300); // 300ms de delay
            _searchTimer.Elapsed += SearchTimer_Elapsed;
            _searchTimer.AutoReset = false;

            // Inscrever nos eventos de notificação
            _revisaoNotificacaoService.RevisaoAdicionada += OnRevisaoAdicionada;
            _revisaoNotificacaoService.RevisaoAtualizada += OnRevisaoAtualizada;
            _revisaoNotificacaoService.RevisaoRemovida += OnRevisaoRemovida;

            // Inscrever no evento de navegação para atualizar quando voltar
            _navigationService.Navigated += OnNavigated;
        }

        private void OnNavigated(object? sender, UserControl? page)
        {
            // Se estamos voltando para a view de revisões e estávamos em modo revisão, recarregar a lista
            if (_emModoRevisao && page is ViewRevisoesClassicas)
            {
                _ = Task.Run(async () => await CarregarRevisoesAsync());
                _emModoRevisao = false;
            }
        }

        public async Task InitializeAsync()
        {
            await CarregarRevisoesAsync();
        }

        [RelayCommand]
        private async Task CarregarRevisoesAsync()
        {
            try
            {
                Carregando = true;
                IsCarregando = true;

                var tiposClassicos = new List<TipoRevisaoEnum>
                {
                    TipoRevisaoEnum.Classico24h,
                    TipoRevisaoEnum.Classico7d,
                    TipoRevisaoEnum.Classico30d,
                    TipoRevisaoEnum.Classico90d,
                    TipoRevisaoEnum.Classico120d,
                    TipoRevisaoEnum.Classico180d
                };

                var resultado = await _revisaoService.ObterRevisoesPendentesAsync(
                    tiposClassicos, PaginaAtual, ItensPorPagina, SearchText);

                var itemsList = resultado.Items?.ToList() ?? new List<Revisao>();

                RevisoesPendentes.Clear();
                foreach (var revisao in itemsList)
                {
                    RevisoesPendentes.Add(revisao);
                }

                TotalItens = resultado.TotalItems;
                FilteredCount = resultado.TotalItems;
                TotalPaginas = (int)Math.Ceiling((double)resultado.TotalItems / ItensPorPagina);
            }
            catch (Exception)
            {
            }
            finally
            {
                Carregando = false;
                IsCarregando = false;
            }
        }

        [RelayCommand]
        private async Task IniciarRevisaoAsync(Revisao? revisao)
        {
            if (revisao == null) return;

            try
            {
                // Marcar que estamos entrando em modo revisão
                _emModoRevisao = true;

                // ✅ FLUXO: revisao.Id (ex: 42) será passado para EditarEstudoViewModel
                // Lá será armazenado em RevisaoId e usado para marcar a revisão como concluída
                // quando o novo estudo for salvo. Veja: EditarEstudoViewModel.InicializarModoRevisaoAsync()
                
                // Obter dados da revisão
                var estudoOrigem = await _estudoService.ObterPorIdAsync(revisao.EstudoOrigemId);
                if (estudoOrigem == null) return;

                // Carregar disciplinas e tipo de revisão em paralelo
                var disciplinasTask = _disciplinaService.ObterTodasAsync();
                var tipoRevisaoTask = _estudoService.ObterTipoEstudoPorNomeAsync("Revisão");
                
                await Task.WhenAll(disciplinasTask, tipoRevisaoTask);
                
                var disciplinas = disciplinasTask.Result;
                var disciplina = disciplinas.FirstOrDefault(d => d.Id == estudoOrigem.Assunto.DisciplinaId);
                var assunto = estudoOrigem.Assunto;
                var tipoRevisao = tipoRevisaoTask.Result;

                if (disciplina == null || assunto == null || tipoRevisao == null)
                {
                    _notificationService.ShowError("Erro", "Não foi possível obter os dados da revisão.");
                    _emModoRevisao = false;
                    return;
                }

                // Criar ViewModel para edição com modo revisão
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

                // Inicializar modo revisão
                await viewModel.InicializarModoRevisaoAsync(disciplina, assunto, tipoRevisao, revisao.Id);

                // Navegar para a view de edição
                var view = new Views.ViewEstudoEditar { DataContext = viewModel };
                _navigationService.NavigateTo(view);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Iniciar", $"Erro ao iniciar revisão: {ex.Message}");
                _emModoRevisao = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
        private async Task FirstPageAsync()
        {
            PaginaAtual = 1;
            await CarregarRevisoesAsync();
        }

        private bool CanGoToFirstPage() => PaginaAtual > 1;

        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        private async Task PreviousPageAsync()
        {
            PaginaAtual--;
            await CarregarRevisoesAsync();
        }

        private bool CanGoToPreviousPage() => PaginaAtual > 1;

        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        private async Task NextPageAsync()
        {
            PaginaAtual++;
            await CarregarRevisoesAsync();
        }

        private bool CanGoToNextPage() => PaginaAtual < TotalPaginas;

        [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
        private async Task LastPageAsync()
        {
            PaginaAtual = TotalPaginas;
            await CarregarRevisoesAsync();
        }

        private bool CanGoToLastPage() => PaginaAtual < TotalPaginas;

        [RelayCommand]
        private async Task RecarregarAsync()
        {
            SearchText = string.Empty;
            PaginaAtual = 1;
            await CarregarRevisoesAsync();
        }

        [RelayCommand]
        private async Task ExcluirRevisaoAsync(Revisao? revisao)
        {
            if (revisao == null) return;

            try
            {
                // Confirmar exclusão com o usuário
                var resultado = _notificationService.ShowConfirmation(
                    "Confirmar Exclusão",
                    $"Tem certeza que deseja excluir esta revisão?\n\n" +
                    $"📚 Assunto: {revisao.EstudoOrigem?.Assunto?.Nome}\n" +
                    $"📅 Tipo: {ObterBadgeTipoRevisao(revisao.TipoRevisao)}\n" +
                    $"📆 Data: {revisao.DataProgramada:dd/MM/yyyy}");

                if (resultado != ToastMessageBoxResult.Yes)
                    return;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Excluindo revisão ID: {revisao.Id}, Tipo: {revisao.TipoRevisao}");

                // Excluir a revisão individual
                bool sucesso = await _revisaoService.ExcluirRevisaoAsync(revisao.Id);

                if (sucesso)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Revisão {revisao.Id} excluída com sucesso");
                    
                    // Recarregar a lista de revisões
                    await CarregarRevisoesAsync();
                    
                    // Feedback ao usuário
                    _notificationService.ShowSuccess("Sucesso", "✅ Revisão excluída com sucesso!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Falha ao excluir revisão {revisao.Id}");
                    
                    _notificationService.ShowError("Erro ao Excluir", "❌ Não foi possível excluir a revisão.\n\nPossíveis motivos:\n• A revisão já foi concluída\n• A revisão não foi encontrada\n• Erro de conexão com o banco");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Erro ao excluir revisão: {ex.Message}");
                
                _notificationService.ShowError("Erro ao Excluir", $"❌ Erro ao excluir revisão: {ex.Message}");
            }
        }

        // Propriedades auxiliares para UI
        public string ObterBadgeTipoRevisao(TipoRevisaoEnum tipo)
        {
            return tipo switch
            {
                TipoRevisaoEnum.Classico24h => "24h",
                TipoRevisaoEnum.Classico7d => "7d",
                TipoRevisaoEnum.Classico30d => "30d",
                TipoRevisaoEnum.Classico90d => "90d",
                TipoRevisaoEnum.Classico120d => "120d",
                TipoRevisaoEnum.Classico180d => "180d",
                _ => tipo.ToString()
            };
        }

        public bool EstaAtrasada(DateTime dataProgramada)
        {
            return dataProgramada.Date < DateTime.Today;
        }

        /// <summary>
        /// Manipulador para quando uma revisão é adicionada
        /// Invalida o cache para forçar recarregamento na próxima página
        /// </summary>
        private void OnRevisaoAdicionada(object? sender, RevisaoEventArgs e)
        {
            _cacheRevisoes = null;
            // Recarregar apenas se estamos na primeira página ou se a revisão pertence aos tipos filtrados
            if (PaginaAtual == 1)
            {
                _ = Task.Run(async () => await CarregarRevisoesAsync());
            }
        }

        /// <summary>
        /// Manipulador para quando uma revisão é atualizada
        /// Atualiza incrementalmente o item na coleção
        /// </summary>
        private void OnRevisaoAtualizada(object? sender, RevisaoEventArgs e)
        {
            if (e.Revisao == null) return;
            
            _cacheRevisoes = null;
            
            // Procurar e atualizar o item na coleção atual
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var itemExistente = RevisoesPendentes.FirstOrDefault(r => r.Id == e.Revisao.Id);
                if (itemExistente != null)
                {
                    // Atualizar propriedades do item existente
                    itemExistente.DataProgramada = e.Revisao.DataProgramada;
                    itemExistente.TipoRevisao = e.Revisao.TipoRevisao;
                }
            });
        }

        /// <summary>
        /// Manipulador para quando uma revisão é removida
        /// Remove incrementalmente o item da coleção
        /// </summary>
        private void OnRevisaoRemovida(object? sender, RevisaoEventArgs e)
        {
            if (e.Revisao == null) return;
            
            _cacheRevisoes = null;
            
            // Remover o item da coleção
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var itemParaRemover = RevisoesPendentes.FirstOrDefault(r => r.Id == e.Revisao.Id);
                if (itemParaRemover != null)
                {
                    RevisoesPendentes.Remove(itemParaRemover);
                    TotalItens--;
                    TotalPaginas = (int)Math.Ceiling((double)TotalItens / ItensPorPagina);
                }
            });
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
                await CarregarRevisoesAsync();
            });
        }
    }
}
