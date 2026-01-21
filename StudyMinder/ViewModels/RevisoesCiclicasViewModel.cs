using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Navigation;
using StudyMinder.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StudyMinder.ViewModels
{
    public partial class RevisoesCiclicasViewModel : BaseViewModel, IEditableViewModel
    {
        private readonly RevisaoService _revisaoService;
        private readonly RevisaoCicloAtivoService _revisaoCicloAtivoService;
        private readonly EstudoService _estudoService;
        private readonly NavigationService _navigationService;
        private readonly RevisaoNotificacaoService _revisaoNotificacaoService;
        private readonly TipoEstudoService _tipoEstudoService;
        private readonly AssuntoService _assuntoService;
        private readonly DisciplinaService _disciplinaService;
        private readonly EstudoTransactionService _transactionService;
        private readonly INotificationService _notificationService;
        private readonly IConfigurationService _configurationService;
        private readonly System.Timers.Timer _searchTimer;

        /// <summary>
        /// Dicionário para rastrear seleções de assuntos em modo de edição (persiste ao mudar de página)
        /// </summary>
        private Dictionary<int, bool> _selecoesPorAssuntoId = new();

        /// <summary>
        /// Cache de assuntos ativos para evitar recarregamentos desnecessários
        /// </summary>
        private List<Assunto>? _cacheAssuntosAtivos;

        /// <summary>
        /// Cache de assuntos disponíveis para evitar recarregamentos desnecessários
        /// </summary>
        private List<Assunto>? _cacheAssuntosDisponiveis;

        [ObservableProperty]
        private ObservableCollection<Assunto> _assuntosDisponiveis = new();

        [ObservableProperty]
        private ObservableCollection<Assunto> _assuntosAtivos = new();

        [ObservableProperty]
        private ObservableCollection<Assunto> _assuntosSelecionados = new();

        [ObservableProperty]
        private ObservableCollection<Assunto> _assuntosParaRemover = new();

        [ObservableProperty]
        private string _textoPesquisaDisponiveis = string.Empty;

        [ObservableProperty]
        private string _textoPesquisaAtivos = string.Empty;

        [ObservableProperty]
        private bool _carregando = false;

        [ObservableProperty]
        private bool _mostrandoAtivos = true;

        [ObservableProperty]
        private string _tituloLista = "Assuntos no Ciclo Ativo";

        [ObservableProperty]
        private AssuntoComDisciplina? _primeiroAssuntoFila;

        [ObservableProperty]
        private bool _isEditingAssuntos = false;

        [ObservableProperty]
        private int _paginaAtual = 1;

        [ObservableProperty]
        private int _totalPaginas = 1;

        [ObservableProperty]
        private int _totalItens = 0;

        [ObservableProperty]
        private int _itensPorPagina = 20;

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
        private ObservableCollection<AssuntoSelecionavel> _assuntosDisponiveisSelecionaveis = new();

        [ObservableProperty]
        private ObservableCollection<Assunto> _assuntosPaginados = new();

        [ObservableProperty]
        private bool _isCarregando = false;

        [ObservableProperty]
        private int _filteredCount = 0;

        public ICollectionView? AssuntosDisponiveisView { get; private set; }

        /// <summary>
        /// Preenche o status do ciclo para uma lista de assuntos
        /// </summary>
        private void PreencherStatusCiclo(IEnumerable<Assunto> assuntos)
        {
            foreach (var assunto in assuntos)
            {
                // Se o assunto já foi estudado, não exibe badge (string vazia)
                if (assunto.DataUltimoEstudo != null)
                {
                    assunto.StatusNoCiclo = "";
                }
                // Verificar se é o mais antigo da lista (o próximo a ser estudado)
                else if (PrimeiroAssuntoFila?.Assunto != null && PrimeiroAssuntoFila.Assunto.Id == assunto.Id)
                {
                    assunto.StatusNoCiclo = "Próximo";
                }
                // Se nunca foi estudado e não é o próximo
                else
                {
                    assunto.StatusNoCiclo = "Nunca Estudado";
                }
            }
        }

        /// <summary>
        /// Determina o status de um assunto no ciclo cíclico
        /// </summary>
        public string ObterStatusAssuntoNoCiclo(Assunto assunto)
        {
            if (assunto == null) return "Desconhecido";

            // Verificar se o assunto nunca foi estudado
            if (assunto.DataUltimoEstudo == null)
                return "Nunca Estudado";

            // Verificar se é o primeiro da fila (próximo a ser estudado)
            var primeiroAssunto = PrimeiroAssuntoFila?.Assunto;
            if (primeiroAssunto != null && primeiroAssunto.Id == assunto.Id)
                return "Próximo";

            // Se chegou aqui, já foi estudado mas não é o próximo
            return "Já Estudado";
        }

        partial void OnIsEditingAssuntosChanged(bool value)
        {
            OnPropertyChanged(nameof(PaginaAtual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(TotalItens));

            FirstPageCommand?.NotifyCanExecuteChanged();
            PreviousPageCommand?.NotifyCanExecuteChanged();
            NextPageCommand?.NotifyCanExecuteChanged();
            LastPageCommand?.NotifyCanExecuteChanged();
        }

        partial void OnPaginaAtualChanged(int value)
        {
            FirstPageCommand?.NotifyCanExecuteChanged();
            PreviousPageCommand?.NotifyCanExecuteChanged();
            NextPageCommand?.NotifyCanExecuteChanged();
            LastPageCommand?.NotifyCanExecuteChanged();
        }

        partial void OnTotalPaginasChanged(int value)
        {
            FirstPageCommand?.NotifyCanExecuteChanged();
            PreviousPageCommand?.NotifyCanExecuteChanged();
            NextPageCommand?.NotifyCanExecuteChanged();
            LastPageCommand?.NotifyCanExecuteChanged();
        }

        public RevisoesCiclicasViewModel(
            RevisaoService revisaoService,
            RevisaoCicloAtivoService revisaoCicloAtivoService,
            EstudoService estudoService,
            NavigationService navigationService,
            RevisaoNotificacaoService revisaoNotificacaoService,
            TipoEstudoService tipoEstudoService = null!,
            AssuntoService assuntoService = null!,
            DisciplinaService disciplinaService = null!,
            EstudoTransactionService transactionService = null!,
            INotificationService notificationService = null!,
            IConfigurationService configurationService = null!)
        {
            _revisaoService = revisaoService;
            _revisaoCicloAtivoService = revisaoCicloAtivoService;
            _estudoService = estudoService;
            _navigationService = navigationService;
            _revisaoNotificacaoService = revisaoNotificacaoService;
            _tipoEstudoService = tipoEstudoService;
            _assuntoService = assuntoService;
            _disciplinaService = disciplinaService;
            _transactionService = transactionService;
            _notificationService = notificationService ?? NotificationService.Instance;
            _configurationService = configurationService ?? new ConfigurationService();

            // Timer para debounce da pesquisa
            _searchTimer = new System.Timers.Timer(300); // 300ms de delay
            _searchTimer.Elapsed += SearchTimer_Elapsed;
            _searchTimer.AutoReset = false;

            // Inscrever nos eventos de notificação
            _revisaoNotificacaoService.AssuntoAdicionadoAoCiclo += OnAssuntoAdicionadoAoCiclo;
            _revisaoNotificacaoService.AssuntoRemovidoDoCiclo += OnAssuntoRemovidoDoCiclo;
            _revisaoNotificacaoService.RevisaoAdicionada += OnRevisaoAdicionada;
            _revisaoNotificacaoService.RevisaoAtualizada += OnRevisaoAtualizada;
            _revisaoNotificacaoService.RevisaoRemovida += OnRevisaoRemovida;

            // Inscrever no evento de navegação para carregar dados quando a view estiver pronta
            _navigationService.Navigated += OnNavigated;
        }

        [RelayCommand]
        private async Task ToggleEditAssuntosCommand()
        {
            if (!IsEditingAssuntos)
            {
                // CORREÇÃO CRÍTICA: 
                // Inicializar o dicionário com TODOS os assuntos ativos atuais.
                // Isso impede que assuntos ativos fora da página atual sejam considerados "removidos" ao salvar.
                _selecoesPorAssuntoId.Clear();
                foreach (var assunto in AssuntosAtivos)
                {
                    _selecoesPorAssuntoId[assunto.Id] = true;
                }

                PaginaAtual = 1;

                // Calcular paginação para assuntos ativos + apenas assuntos disponíveis CONCLUÍDOS
                var assuntosDisponiveisConcluidos = AssuntosDisponiveis.Where(a => a.Concluido).ToList();

                // Nota: AssuntosAtivos já estão incluídos no count, mas a lista combinada pode ter duplicatas
                // se a lógica de união não for cuidadosa, mas aqui usamos a contagem para paginação.
                // O ideal é basear o TotalItens na lista combinada real que será gerada em CarregarAssuntos...
                // Mas para inicializar, podemos estimar ou deixar o Carregar... atualizar o TotalItens.

                CarregarAssuntosDisponiveisParaSelecao();
                SearchText = string.Empty;
                IsEditingAssuntos = true;

                OnPropertyChanged(nameof(IsEditingAssuntos));
                // TotalItens e TotalPaginas serão atualizados dentro de CarregarAssuntosDisponiveisParaSelecao
            }
            else
            {
                await SalvarAlteracoesAssuntosAsync();
            }
        }

        [RelayCommand]
        private void CancelarEdicaoAssuntosCommand()
        {
            PaginaAtual = 1;
            TotalItens = AssuntosAtivos.Count;
            TotalPaginas = (int)Math.Ceiling((double)TotalItens / ItensPorPagina);

            _selecoesPorAssuntoId.Clear();
            AssuntosDisponiveisView = null;
            SearchText = string.Empty;
            IsEditingAssuntos = false;

            OnPropertyChanged(nameof(TotalItens));
            OnPropertyChanged(nameof(TotalPaginas));
        }

        private async Task SalvarAlteracoesAssuntosAsync()
        {
            try
            {
                Carregando = true;
                IsCarregando = true;

                var assuntosSelecionadosIds = _selecoesPorAssuntoId
                    .Where(kvp => kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToHashSet();

                var assuntosAtivosIds = AssuntosAtivos
                    .Select(a => a.Id)
                    .ToHashSet();

                var novasSelecoes = assuntosSelecionadosIds.Except(assuntosAtivosIds).ToList();
                var remocoes = assuntosAtivosIds.Except(assuntosSelecionadosIds).ToList();

                var errosAdicao = new List<(int Id, string Mensagem)>();
                var errosRemocao = new List<(int Id, string Mensagem)>();

                foreach (var assuntoId in novasSelecoes)
                {
                    var resultado = await _revisaoCicloAtivoService.AdicionarAssuntoAoCicloAsync(assuntoId);
                    if (!resultado.Success)
                    {
                        errosAdicao.Add((assuntoId, resultado.ErrorMessage));
                    }
                }

                foreach (var assuntoId in remocoes)
                {
                    var resultado = await _revisaoCicloAtivoService.RemoverAssuntoDoCicloAsync(assuntoId);
                    if (!resultado.Success)
                    {
                        errosRemocao.Add((assuntoId, resultado.ErrorMessage));
                    }
                }

                // Verificar se houve erros
                if (errosAdicao.Count > 0 || errosRemocao.Count > 0)
                {
                    var mensagem = "Erros ao salvar alterações:\n\n";

                    if (errosAdicao.Count > 0)
                    {
                        mensagem += $"❌ Falha ao adicionar {errosAdicao.Count} assunto(s):\n";
                        foreach (var erro in errosAdicao.Take(3)) // Limita a 3 erros para não poluir
                        {
                            mensagem += $"   • Assunto {erro.Id}: {erro.Mensagem}\n";
                        }
                        if (errosAdicao.Count > 3)
                            mensagem += $"   • ... e mais {errosAdicao.Count - 3} erro(s)\n";
                        mensagem += "\n";
                    }

                    if (errosRemocao.Count > 0)
                    {
                        mensagem += $"❌ Falha ao remover {errosRemocao.Count} assunto(s):\n";
                        foreach (var erro in errosRemocao.Take(3)) // Limita a 3 erros para não poluir
                        {
                            mensagem += $"   • Assunto {erro.Id}: {erro.Mensagem}\n";
                        }
                        if (errosRemocao.Count > 3)
                            mensagem += $"   • ... e mais {errosRemocao.Count - 3} erro(s)\n";
                    }

                    _notificationService.ShowError("Erro ao Salvar", mensagem);
                }

                await CarregarAssuntosAsync();
                await CarregarFilaCiclicaAsync();

                PaginaAtual = 1;
                TotalItens = AssuntosAtivos.Count;
                TotalPaginas = (int)Math.Ceiling((double)TotalItens / ItensPorPagina);

                AtualizarAssuntosPaginados();

                _selecoesPorAssuntoId.Clear();
                AssuntosDisponiveisView = null;
                SearchText = string.Empty;
                IsEditingAssuntos = false;

                OnPropertyChanged(nameof(TotalItens));
                OnPropertyChanged(nameof(TotalPaginas));
                OnPropertyChanged(nameof(AssuntosPaginados));

                if (errosAdicao.Count == 0 && errosRemocao.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] ✅ SalvarAlteracoesAssuntosAsync concluído com sucesso");

                    // Exibir mensagem de sucesso
                    _notificationService.ShowSuccess("Sucesso", "Alterações salvas com sucesso!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Erro ao salvar alterações: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                _notificationService.ShowError("Erro ao Salvar", $"Erro ao salvar alterações:\n{ex.Message}");
            }
            finally
            {
                Carregando = false;
                IsCarregando = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanNavigateToFirstPage))]
        private void FirstPage()
        {
            if (PaginaAtual > 1)
            {
                PaginaAtual = 1;
                if (IsEditingAssuntos)
                    CarregarAssuntosDisponiveisParaSelecao();
                else if (!string.IsNullOrWhiteSpace(SearchText))
                    AtualizarAssuntosPaginadosComFiltro();
                else
                    _ = CarregarAssuntosAsync();
            }
        }

        private bool CanNavigateToFirstPage() => TotalPaginas > 1 && PaginaAtual > 1;

        [RelayCommand(CanExecute = nameof(CanNavigateToPreviousPage))]
        private void PreviousPage()
        {
            if (PaginaAtual > 1)
            {
                PaginaAtual--;
                if (IsEditingAssuntos)
                    CarregarAssuntosDisponiveisParaSelecao();
                else if (!string.IsNullOrWhiteSpace(SearchText))
                    AtualizarAssuntosPaginadosComFiltro();
                else
                    _ = CarregarAssuntosAsync();
            }
        }

        private bool CanNavigateToPreviousPage() => TotalPaginas > 1 && PaginaAtual > 1;

        [RelayCommand(CanExecute = nameof(CanNavigateToNextPage))]
        private void NextPage()
        {
            if (PaginaAtual < TotalPaginas)
            {
                PaginaAtual++;
                if (IsEditingAssuntos)
                    CarregarAssuntosDisponiveisParaSelecao();
                else if (!string.IsNullOrWhiteSpace(SearchText))
                    AtualizarAssuntosPaginadosComFiltro();
                else
                    _ = CarregarAssuntosAsync();
            }
        }

        private bool CanNavigateToNextPage() => TotalPaginas > 1 && PaginaAtual < TotalPaginas;

        [RelayCommand(CanExecute = nameof(CanNavigateToLastPage))]
        private void LastPage()
        {
            if (PaginaAtual < TotalPaginas)
            {
                PaginaAtual = TotalPaginas;
                if (IsEditingAssuntos)
                    CarregarAssuntosDisponiveisParaSelecao();
                else if (!string.IsNullOrWhiteSpace(SearchText))
                    AtualizarAssuntosPaginadosComFiltro();
                else
                    _ = CarregarAssuntosAsync();
            }
        }

        private bool CanNavigateToLastPage() => TotalPaginas > 1 && PaginaAtual < TotalPaginas;

        private void OnNavigated(object? sender, UserControl? page)
        {
            // Carregar dados quando navegar para a ViewRevisoesCiclicas
            if (page is Views.ViewRevisoesCiclicas)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Navegando para ViewRevisoesCiclicas, carregando dados...");
                // Usar Dispatcher para garantir execução no UI thread
                Dispatcher.CurrentDispatcher.BeginInvoke(async () =>
                {
                    await Task.Delay(50); // Pequeno delay para garantir que a UI está pronta
                    await InitializeAsync();
                });
            }
        }

        public async Task InitializeAsync()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] InitializeAsync iniciado");
            await CarregarAssuntosAsync();
            await CarregarFilaCiclicaAsync();
        }

        private async Task CarregarFilaCiclicaAsync()
        {
            try
            {
                Carregando = true;
                IsCarregando = true;

                // Carregar apenas o primeiro assunto da fila (otimizado)
                var primeiroAssunto = await _revisaoService.ObterPrimeiroAssuntoCicloAsync();

                if (primeiroAssunto != null)
                {
                    PrimeiroAssuntoFila = new AssuntoComDisciplina
                    {
                        Assunto = primeiroAssunto,
                        Disciplina = primeiroAssunto.Disciplina?.Nome ?? "Sem Disciplina",
                        PosicaoFila = 1,
                        DataUltimoEstudo = primeiroAssunto.DataUltimoEstudo,
                        IsPrimeiroDaFila = true
                    };
                }
                else
                {
                    PrimeiroAssuntoFila = null;
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                Carregando = false;
                IsCarregando = false;
            }
        }

        [RelayCommand]
        private async Task CarregarFilaCiclicaCommandAsync()
        {
            await CarregarFilaCiclicaAsync();
        }

        [RelayCommand]
        private async Task IniciarEstudoFilaAsync(Assunto? assunto)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] ========== IniciarEstudoFilaAsync INICIADO ==========");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Parâmetro recebido: {(assunto == null ? "NULL" : $"Assunto ID={assunto.Id}, Nome={assunto.Nome}")}");

            if (assunto == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] ❌ Assunto é NULL, retornando...");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Iniciando estudo cíclico para assunto: {assunto.Nome}");

                System.Diagnostics.Debug.WriteLine("[DEBUG] 1️⃣ Carregando todos os assuntos do banco...");
                var assuntos = await _assuntoService.ObterTodosAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 1️⃣ ✅ {assuntos.Count} assuntos carregados");

                System.Diagnostics.Debug.WriteLine($"[DEBUG] 2️⃣ Procurando assunto com ID={assunto.Id}...");
                var assuntoCompleto = assuntos.FirstOrDefault(a => a.Id == assunto.Id);
                if (assuntoCompleto == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Assunto com ID={assunto.Id} não encontrado!");
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 2️⃣ ✅ Assunto encontrado: {assuntoCompleto.Nome}");

                // Obter disciplina
                System.Diagnostics.Debug.WriteLine("[DEBUG] 3️⃣ Procurando disciplina com ID={assuntoCompleto.DisciplinaId}...");
                var disciplinas = await _disciplinaService.ObterTodasAsync();
                var disciplina = disciplinas.FirstOrDefault(d => d.Id == assuntoCompleto.DisciplinaId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 3️⃣ ✅ Disciplina encontrada: {disciplina.Nome}");

                // Carregar tipo de estudo "Revisão" (mesmo padrão do HomeViewModel)
                System.Diagnostics.Debug.WriteLine("[DEBUG] 4️⃣ Procurando tipo de estudo 'Revisão'...");
                var tipoRevisao = await _estudoService.ObterTipoEstudoPorNomeAsync("Revisão");
                if (tipoRevisao == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] ❌ Tipo de estudo 'Revisão' não encontrado!");
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 4️⃣ ✅ Tipo de estudo encontrado: {tipoRevisao.Nome} (ID={tipoRevisao.Id})");

                // Criar ViewModel para edição em modo revisão
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

                System.Diagnostics.Debug.WriteLine("[DEBUG] 5️⃣ ✅ EditarEstudoViewModel criado");

                // Inicializar modo revisão (sem ser uma revisão agendada específica)
                System.Diagnostics.Debug.WriteLine("[DEBUG] 6️⃣ Inicializando modo revisão...");
                await viewModel.InicializarModoRevisaoAsync(disciplina, assuntoCompleto, tipoRevisao, 0);
                System.Diagnostics.Debug.WriteLine("[DEBUG] 6️⃣ ✅ Modo revisão inicializado");

                // Navegar para a view de edição
                System.Diagnostics.Debug.WriteLine("[DEBUG] 7️⃣ Navegando para ViewEstudoEditar...");
                var view = new Views.ViewEstudoEditar { DataContext = viewModel };
                _navigationService.NavigateTo(view);
                System.Diagnostics.Debug.WriteLine("[DEBUG] 7️⃣ ✅ Navegação concluída");
                System.Diagnostics.Debug.WriteLine("[DEBUG] ========== IniciarEstudoFilaAsync CONCLUÍDO COM SUCESSO ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ EXCEÇÃO em IniciarEstudoFilaAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Inner Exception: {ex.InnerException.Message}");
                _notificationService.ShowError("Erro ao Iniciar", $"Erro ao iniciar estudo: {ex.Message}");
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] ========== IniciarEstudoFilaAsync FINALIZADO ==========");
            }
        }

        [RelayCommand]
        private async Task RemoverAssuntoDoCicloCommand(Assunto? assunto)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] ========== RemoverAssuntoDoCicloCommand INICIADO ==========");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Parâmetro recebido: {(assunto == null ? "NULL" : $"Assunto ID={assunto.Id}, Nome={assunto.Nome}")}");

            if (assunto == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] ❌ Assunto é NULL, retornando...");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Removendo assunto do ciclo: {assunto.Nome}");

                System.Diagnostics.Debug.WriteLine($"[DEBUG] 1️⃣ Chamando RemoverAssuntoDoCicloAsync com ID={assunto.Id}...");
                var resultado = await _revisaoCicloAtivoService.RemoverAssuntoDoCicloAsync(assunto.Id);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 1️⃣ ✅ Serviço respondeu: Success={resultado.Success}");

                if (resultado.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Assunto removido com sucesso: {assunto.Nome}");
                    _notificationService.ShowSuccess("Sucesso", $"Assunto '{assunto.Nome}' removido do ciclo com sucesso!");

                    // Recarregar dados
                    System.Diagnostics.Debug.WriteLine("[DEBUG] 2️⃣ Recarregando dados...");
                    await CarregarAssuntosAsync();
                    await CarregarFilaCiclicaAsync();
                    System.Diagnostics.Debug.WriteLine("[DEBUG] 2️⃣ ✅ Dados recarregados");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] ❌ Erro ao remover assunto: {resultado.ErrorMessage}");
                    _notificationService.ShowError("Erro ao Remover", $"Erro ao remover assunto do ciclo:\n{resultado.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ❌ EXCEÇÃO em RemoverAssuntoDoCicloCommand: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Inner Exception: {ex.InnerException.Message}");
                _notificationService.ShowError("Erro ao Remover", $"Erro ao remover assunto do ciclo:\n{ex.Message}");
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] ========== RemoverAssuntoDoCicloCommand FINALIZADO ==========");
            }
        }

        private async Task CarregarAssuntosAsync()
        {
            try
            {
                Carregando = true;
                IsCarregando = true;

                // Chamamos os métodos (agora refatorados) pedindo "Página 1"
                // O Serviço ignorará o tamanho e retornará a lista completa e ordenada corretamente.
                var ativosTask = _revisaoCicloAtivoService.ObterAssuntosPaginadoAsync(1, int.MaxValue);
                var disponiveisTask = _revisaoCicloAtivoService.ObterAssuntosDisponiveisPaginadoAsync(1, int.MaxValue);

                await Task.WhenAll(ativosTask, disponiveisTask);

                var resultadoAtivos = ativosTask.Result;
                var resultadoDisponiveis = disponiveisTask.Result;

                // Executar na thread de UI
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // 1. Atualizar Lista de Ativos
                    AssuntosAtivos.Clear();
                    foreach (var assunto in resultadoAtivos.Items)
                    {
                        AssuntosAtivos.Add(assunto);
                    }
                    PreencherStatusCiclo(AssuntosAtivos);

                    // 2. Atualizar Lista de Disponíveis
                    AssuntosDisponiveis.Clear();
                    foreach (var assunto in resultadoDisponiveis.Items)
                    {
                        AssuntosDisponiveis.Add(assunto);
                    }
                    // Opcional: Se quiser filtrar por texto de pesquisa nos disponíveis AQUI também:
                    // (Mas o serviço já trouxe apenas os concluídos)

                    // 3. Atualizar Totais para a UI
                    // Como carregamos tudo, o total é a contagem da lista
                    if (IsEditingAssuntos)
                    {
                        // Modo Edição: Mostra Ativos + Disponíveis Concluídos (que já vieram filtrados)
                        TotalItens = AssuntosAtivos.Count + AssuntosDisponiveis.Count;
                    }
                    else
                    {
                        // Modo Normal: Apenas ativos
                        TotalItens = AssuntosAtivos.Count;
                    }

                    // Calculamos as páginas com base no total real (para a paginação VISUAL funcionar)
                    TotalPaginas = (int)Math.Ceiling((double)TotalItens / ItensPorPagina);
                    if (TotalPaginas < 1) TotalPaginas = 1;

                    if (PaginaAtual > TotalPaginas) PaginaAtual = 1;

                    // 4. Atualiza a lista visual (AssuntosPaginados)
                    if (!string.IsNullOrWhiteSpace(SearchText) && !IsEditingAssuntos)
                    {
                        AtualizarAssuntosPaginadosComFiltro();
                    }
                    else
                    {
                        AtualizarAssuntosPaginados();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar assuntos: {ex.Message}");
            }
            finally
            {
                Carregando = false;
                IsCarregando = false;
            }
        }

        private void CarregarAssuntosDisponiveisParaSelecao()
        {
            try
            {
                Carregando = true;
                IsCarregando = true;

                Action<int, bool> onSelectionChanged = (assuntoId, isSelected) =>
                {
                    _selecoesPorAssuntoId[assuntoId] = isSelected;
                };

                // 1. COMBINAÇÃO
                // Pega os ativos + disponíveis (que já foram filtrados por "Concluído" no carregamento anterior)
                var assuntosDisponiveisConcluidos = AssuntosDisponiveis.Where(a => a.Concluido).ToList();
                var todosAssuntos = AssuntosAtivos.Concat(assuntosDisponiveisConcluidos);

                // 2. FILTRO DE PESQUISA
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    todosAssuntos = todosAssuntos.Where(a =>
                        StringNormalizationHelper.ContainsIgnoreCaseAndAccents(a.Nome, SearchText) ||
                        (a.Disciplina != null && StringNormalizationHelper.ContainsIgnoreCaseAndAccents(a.Disciplina.Nome, SearchText))
                    );
                }

                // 3. ORDENAÇÃO (CORREÇÃO AQUI)
                // Ordena a lista FINAL combinada por Disciplina e depois por Nome
                var listaFinalOrdenada = todosAssuntos
                    .OrderBy(a => a.Nome)
                    .ToList();

                // Atualizar contadores
                TotalItens = listaFinalOrdenada.Count;
                TotalPaginas = (int)Math.Ceiling((double)TotalItens / ItensPorPagina);
                if (TotalPaginas < 1) TotalPaginas = 1;

                if (PaginaAtual > TotalPaginas) PaginaAtual = TotalPaginas;

                // Paginação da memória
                var startIndex = (PaginaAtual - 1) * ItensPorPagina;
                var endIndex = Math.Min(startIndex + ItensPorPagina, listaFinalOrdenada.Count);
                if (startIndex < 0) startIndex = 0;

                AssuntosDisponiveisSelecionaveis.Clear();

                // Criar os objetos selecionáveis para a página atual
                // Note que iteramos sobre 'listaFinalOrdenada', não mais sobre a concatenação bruta
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (i >= listaFinalOrdenada.Count) break;

                    var assunto = listaFinalOrdenada[i];

                    // Manter estado de seleção
                    bool isSelected = _selecoesPorAssuntoId.ContainsKey(assunto.Id)
                        ? _selecoesPorAssuntoId[assunto.Id]
                        : AssuntosAtivos.Any(a => a.Id == assunto.Id);

                    var selecionavel = new AssuntoSelecionavel(assunto, isSelected, onSelectionChanged);
                    AssuntosDisponiveisSelecionaveis.Add(selecionavel);

                    // Garante que o estado inicial esteja no dicionário
                    if (!_selecoesPorAssuntoId.ContainsKey(assunto.Id))
                    {
                        _selecoesPorAssuntoId[assunto.Id] = isSelected;
                    }
                }

                AssuntosDisponiveisView = CollectionViewSource.GetDefaultView(AssuntosDisponiveisSelecionaveis);
                IsEditingAssuntos = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Erro em CarregarAssuntosDisponiveisParaSelecao: {ex.Message}");
            }
            finally
            {
                Carregando = false;
                IsCarregando = false;
            }
        }

        private void AtualizarAssuntosPaginados()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] AtualizarAssuntosPaginados iniciado");
            AssuntosPaginados.Clear();

            // Como AssuntosAtivos agora tem a lista COMPLETA, precisamos paginar aqui
            var startIndex = (PaginaAtual - 1) * ItensPorPagina;

            // Proteção contra índice inválido
            if (startIndex < 0) startIndex = 0;

            var assuntosPagina = AssuntosAtivos
                .Skip(startIndex)
                .Take(ItensPorPagina)
                .ToList();

            foreach (var assunto in assuntosPagina)
            {
                AssuntosPaginados.Add(assunto);
            }

            // Se não há filtro, o total exibido é o da página
            FilteredCount = AssuntosPaginados.Count;

            // Atualizar TotalItens/Paginas caso não tenha busca (para garantir sincronia)
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                TotalItens = AssuntosAtivos.Count;
                TotalPaginas = (int)Math.Ceiling((double)TotalItens / ItensPorPagina);
            }
        }

        private void AtualizarAssuntosPaginadosComFiltro()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] AtualizarAssuntosPaginadosComFiltro iniciado");
            AssuntosPaginados.Clear();

            // Filtrar assuntos baseado no texto de pesquisa (case-insensitive e sem acentos)
            var assuntosFiltrados = AssuntosAtivos.Where(a =>
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    return true;

                return StringNormalizationHelper.ContainsIgnoreCaseAndAccents(a.Nome, SearchText) ||
                       (a.Disciplina != null && StringNormalizationHelper.ContainsIgnoreCaseAndAccents(a.Disciplina.Nome, SearchText));
            }).ToList();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Assuntos após filtro: {assuntosFiltrados.Count} de {AssuntosAtivos.Count}");

            // Atualizar totais para paginação
            // IMPORTANTE: Atualizar os totais com base no resultado DO FILTRO
            TotalItens = assuntosFiltrados.Count;
            TotalPaginas = (int)Math.Ceiling((double)TotalItens / ItensPorPagina);

            // Ajustar página atual se necessário
            if (PaginaAtual > TotalPaginas && TotalPaginas > 0) PaginaAtual = 1;

            // Paginação da lista filtrada
            var startIndex = (PaginaAtual - 1) * ItensPorPagina;
            var assuntosPagina = assuntosFiltrados.Skip(startIndex).Take(ItensPorPagina).ToList();

            AssuntosPaginados.Clear();
            foreach (var assunto in assuntosPagina)
            {
                AssuntosPaginados.Add(assunto);
            }

            FilteredCount = AssuntosPaginados.Count;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] AtualizarAssuntosPaginadosComFiltro concluído - Total: {AssuntosPaginados.Count}, FilteredCount: {FilteredCount}");
        }

        /// <summary>
        /// Manipulador para quando um assunto é adicionado ao ciclo ativo
        /// Otimizado para atualizar apenas o assunto adicionado em vez de recarregar tudo
        /// </summary>
        private void OnAssuntoAdicionadoAoCiclo(object? sender, RevisaoCicloAtivoEventArgs e)
        {
            // OTIMIZAÇÃO: Atualizar apenas o assunto adicionado em vez de recarregar tudo
            var assunto = AssuntosDisponiveis.FirstOrDefault(a => a.Id == e.AssuntoId);
            if (assunto != null)
            {
                AssuntosDisponiveis.Remove(assunto);
                AssuntosAtivos.Add(assunto);
                
                // Recalcular paginação
                TotalItens = AssuntosAtivos.Count;
                TotalPaginas = (int)Math.Ceiling((double)TotalItens / ItensPorPagina);
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Assunto {e.AssuntoId} adicionado ao ciclo (otimizado)");
            }
        }

        /// <summary>
        /// Manipulador para quando um assunto é removido do ciclo ativo
        /// Otimizado para atualizar apenas o assunto removido em vez de recarregar tudo
        /// </summary>
        private void OnAssuntoRemovidoDoCiclo(object? sender, RevisaoCicloAtivoEventArgs e)
        {
            // OTIMIZAÇÃO: Atualizar apenas o assunto removido em vez de recarregar tudo
            var assunto = AssuntosAtivos.FirstOrDefault(a => a.Id == e.AssuntoId);
            if (assunto != null)
            {
                AssuntosAtivos.Remove(assunto);
                AssuntosDisponiveis.Add(assunto);
                
                // Recalcular paginação
                TotalItens = AssuntosAtivos.Count;
                TotalPaginas = (int)Math.Ceiling((double)TotalItens / ItensPorPagina);
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Assunto {e.AssuntoId} removido do ciclo (otimizado)");
            }
        }

        /// <summary>
        /// Manipulador para quando uma revisão é adicionada
        /// Invalida o cache para forçar recarregamento
        /// </summary>
        private void OnRevisaoAdicionada(object? sender, RevisaoEventArgs e)
        {
            _cacheAssuntosAtivos = null;
            _cacheAssuntosDisponiveis = null;
        }

        /// <summary>
        /// Manipulador para quando uma revisão é atualizada
        /// Invalida o cache para forçar recarregamento
        /// </summary>
        private void OnRevisaoAtualizada(object? sender, RevisaoEventArgs e)
        {
            _cacheAssuntosAtivos = null;
            _cacheAssuntosDisponiveis = null;
        }

        /// <summary>
        /// Manipulador para quando uma revisão é removida
        /// Invalida o cache para forçar recarregamento
        /// </summary>
        private void OnRevisaoRemovida(object? sender, RevisaoEventArgs e)
        {
            _cacheAssuntosAtivos = null;
            _cacheAssuntosDisponiveis = null;
        }

        /// <summary>
        /// Manipulador do timer de debounce para pesquisa
        /// Executa a pesquisa após 300ms de inatividade
        /// </summary>
        private void SearchTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Executando pesquisa para: '{SearchText}'");

            // Executar na thread da UI para evitar erro de CollectionView
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (IsEditingAssuntos)
                {
                    CarregarAssuntosDisponiveisParaSelecao();
                }
                else
                {
                    AtualizarAssuntosPaginadosComFiltro();
                }
            });
        }

        /// <summary>
        /// Propriedade para rastrear se há alterações não salvas
        /// </summary>
        public bool HasUnsavedChanges
        {
            get
            {
                // Verificar se há seleções pendentes em modo de edição
                if (IsEditingAssuntos && _selecoesPorAssuntoId.Count > 0)
                {
                    // Comparar seleções atuais com estado original
                    var assuntosAtivosIds = AssuntosAtivos.Select(a => a.Id).ToHashSet();
                    var assuntosSelecionadosIds = _selecoesPorAssuntoId
                        .Where(kvp => kvp.Value)
                        .Select(kvp => kvp.Key)
                        .ToHashSet();

                    // Se há diferenças, há alterações não salvas
                    return !assuntosAtivosIds.SetEquals(assuntosSelecionadosIds);
                }

                return false;
            }
        }

        /// <summary>
        /// Implementação de IEditableViewModel - Chamado quando a view está sendo descarregada
        /// Retorna true se deve cancelar a navegação, false caso contrário
        /// </summary>
        public async Task<bool> OnViewUnloadingAsync()
        {
            if (!HasUnsavedChanges)
                return false;

            var resultado = _notificationService.ShowConfirmation(
                "Alterações Não Salvas",
                "Você tem alterações não salvas no ciclo de revisões. Deseja descartá-las?");

            return resultado != ToastMessageBoxResult.Yes;
        }
    }
}
