using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Data;
using StudyMinder.Models;
using StudyMinder.Navigation;
using StudyMinder.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using System.Linq;

namespace StudyMinder.ViewModels
{
    public partial class EditarEstudoViewModel : BaseViewModel, IDisposable, IEditableViewModel
    {
        // Evento para notificar quando um estudo foi salvo
        public event EventHandler? EstudoSalvo;

        private readonly EstudoService _estudoService;
        private readonly TipoEstudoService _tipoEstudoService;
        private readonly AssuntoService _assuntoService;
        private readonly DisciplinaService _disciplinaService;
        private readonly EstudoTransactionService _transactionService;
        private readonly NavigationService _navigationService;
        private readonly RevisaoService _revisaoService;
        private readonly IConfigurationService? _configurationService;
        private readonly INotificationService _notificationService;
        private readonly Estudo? _estudoOriginal;

        [ObservableProperty]
        private ObservableCollection<TipoEstudo> tiposEstudo = new();

        [ObservableProperty]
        private ObservableCollection<Disciplina> disciplinas = new();

        [ObservableProperty]
        private ObservableCollection<Assunto> assuntos = new();

        [ObservableProperty]
        private ObservableCollection<Assunto> todosAssuntos = new();

        [ObservableProperty]
        private TipoEstudo? tipoEstudoSelecionado;

        [ObservableProperty]
        private Disciplina? disciplinaSelecionada;

        [ObservableProperty]
        private Assunto? assuntoSelecionado;

        [ObservableProperty]
        private DateTime dataEstudo = DateTime.Now;

        [ObservableProperty]
        private string duracaoTexto = "00:00:00";

        [ObservableProperty]
        private int acertos = 0;

        [ObservableProperty]
        private int erros = 0;

        [ObservableProperty]
        private int paginaInicial = 0;

        [ObservableProperty]
        private int paginaFinal = 0;

        [ObservableProperty]
        private string? material;

        [ObservableProperty]
        private string? professor;

        [ObservableProperty]
        private string? topicos;

        [ObservableProperty]
        private string? comentarios;

        [ObservableProperty]
        private bool mostrarEstatisticas;

        [ObservableProperty]
        private int totalQuestoes;

        [ObservableProperty]
        private double rendimentoPercentual;

        [ObservableProperty]
        private int totalPaginas;

        [ObservableProperty]
        private bool isSaving;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool hasUnsavedChanges;

        [ObservableProperty]
        private bool isTimerAtivo;

        private bool _minimizarParaBandeja;
        public bool MinimizarParaBandeja
        {
            get => _minimizarParaBandeja;
            set
            {
                if (_minimizarParaBandeja != value)
                {
                    _minimizarParaBandeja = value;
                    OnPropertyChanged(nameof(MinimizarParaBandeja));
                    
                    // Persistir mudança nas configurações
                    if (_configurationService != null)
                    {
                        _configurationService.Settings.Study.MinimizeToTrayOnStart = value;
                        _ = _configurationService.SaveAsync();
                    }
                }
            }
        }

        [ObservableProperty]
        private bool usarPomodoro;

        [ObservableProperty]
        private TimeSpan tempoEstudo = TimeSpan.Zero;

        // Propriedades para controlar modo revisão
        [ObservableProperty]
        private bool isRevisao = false;

        [ObservableProperty]
        private Disciplina? disciplinaRevisao;

        [ObservableProperty]
        private Assunto? assuntoRevisao;

        [ObservableProperty]
        private TipoEstudo? tipoEstudoRevisao;

        [ObservableProperty]
        private int? revisaoId;

        // Propriedades para agendamento de revisão
        [ObservableProperty]
        private ObservableCollection<TipoRevisaoOpcao> tiposRevisao = new();

        [ObservableProperty]
        private TipoRevisaoOpcao? tipoRevisaoSelecionado;

        // Propriedade para marcar assunto como concluído
        [ObservableProperty]
        private bool marcarAssuntoConcluido = false;

        // Propriedades para ritmo de estudo
        [ObservableProperty]
        private int ritmoPaginas = 0;

        [ObservableProperty]
        private int ritmoQuestoes = 0;

        [ObservableProperty]
        private double paginasPorMinuto = 0;

        [ObservableProperty]
        private double questoesPorMinuto = 0;

        // Comandos
        public IAsyncRelayCommand SalvarAsyncCommand { get; }
        public IRelayCommand CancelarCommand { get; }
        public IAsyncRelayCommand SalvarRascunhoAsyncCommand { get; }
        public IRelayCommand IniciarTimerCommand { get; }
        public IRelayCommand PausarTimerCommand { get; }
        public IRelayCommand AlternarTimerCommand { get; }
        public IRelayCommand AlternarTimerEMinimizarCommand { get; }
        public IRelayCommand ReiniciarTimerCommand { get; }
        public IRelayCommand AbrirCadernoQuestoesCommand { get; }

        // Action para solicitar minimização/restauração ao code-behind
        public Action? OnSolicitarMinimizacao { get; set; }
        public Action? OnSolicitarRestauracao { get; set; }

        private readonly DispatcherTimer _timer;
        private DateTime _inicioEstudo;

        // Construtor sem parâmetros para o designer XAML
        public EditarEstudoViewModel()
        {
            _estudoService = null!;
            _tipoEstudoService = null!;
            _assuntoService = null!;
            _disciplinaService = null!;
            _transactionService = null!;
            _navigationService = null!;
            _revisaoService = null!;
            _configurationService = null!;
            _notificationService = null!;
            _estudoOriginal = null;

            Title = "Novo Estudo";
            DataEstudo = DateTime.Now.Date;

            // Inicializar opções de revisão
            InicializarTiposRevisao();
            // Selecionar item vazio por padrão (sem revisão)
            TipoRevisaoSelecionado = TiposRevisao.FirstOrDefault();

            // Inicializar timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // Atualiza a cada segundo
            _timer.Tick += (s, e) => Timer_Elapsed(s, e);

            // Inicializar comandos mesmo para o designer
            SalvarAsyncCommand = new AsyncRelayCommand(SalvarAsync);
            CancelarCommand = new RelayCommand(Cancelar);
            SalvarRascunhoAsyncCommand = new AsyncRelayCommand(SalvarRascunhoAsync);
            IniciarTimerCommand = new RelayCommand(IniciarTimer);
            PausarTimerCommand = new RelayCommand(PausarTimer);
            AlternarTimerCommand = new RelayCommand(AlternarTimer);
            AlternarTimerEMinimizarCommand = new RelayCommand(AlternarTimerEMinimizar);
            ReiniciarTimerCommand = new RelayCommand(ReiniciarTimer);
            AbrirCadernoQuestoesCommand = new RelayCommand(AbrirCadernoQuestoes);

        }

        public EditarEstudoViewModel(
            EstudoService estudoService,
            TipoEstudoService tipoEstudoService,
            AssuntoService assuntoService,
            DisciplinaService disciplinaService,
            EstudoTransactionService transactionService,
            NavigationService navigationService,
            RevisaoService revisaoService,
            INotificationService notificationService,
            IConfigurationService? configurationService = null,
            Estudo? estudo = null)
        {
            _estudoService = estudoService;
            _tipoEstudoService = tipoEstudoService;
            _assuntoService = assuntoService;
            _disciplinaService = disciplinaService;
            _transactionService = transactionService;
            _navigationService = navigationService;
            _revisaoService = revisaoService;
            _notificationService = notificationService;
            _configurationService = configurationService ?? new ConfigurationService();
            _estudoOriginal = estudo;

            // Inicializar timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // Atualiza a cada segundo
            _timer.Tick += (s, e) => Timer_Elapsed(s, e);

            // Inicializar comandos
            SalvarAsyncCommand = new AsyncRelayCommand(SalvarAsync);
            CancelarCommand = new RelayCommand(Cancelar);
            SalvarRascunhoAsyncCommand = new AsyncRelayCommand(SalvarRascunhoAsync);
            IniciarTimerCommand = new RelayCommand(IniciarTimer);
            PausarTimerCommand = new RelayCommand(PausarTimer);
            AlternarTimerCommand = new RelayCommand(AlternarTimer);
            AlternarTimerEMinimizarCommand = new RelayCommand(AlternarTimerEMinimizar);
            ReiniciarTimerCommand = new RelayCommand(ReiniciarTimer);
            AbrirCadernoQuestoesCommand = new RelayCommand(AbrirCadernoQuestoes);

            Title = estudo == null ? "Novo Estudo" : "Editar Estudo";
            DataEstudo = DateTime.Now.Date;

            // Carregar configuração de minimização para bandeja
            if (_configurationService != null)
            {
                _minimizarParaBandeja = _configurationService.Settings.Study.MinimizeToTrayOnStart;
            }

            // Inicializar opções de revisão
            InicializarTiposRevisao();
            // Selecionar item vazio por padrão (sem revisão)
            TipoRevisaoSelecionado = TiposRevisao.FirstOrDefault();

            _ = CarregarDadosAsync();

            if (estudo != null)
            {
                CarregarDadosEstudo(estudo);
            }
        }

        private TimeSpan _tempoAcumulado = TimeSpan.Zero;

        private void Timer_Elapsed(object? sender, EventArgs e)
        {
            if (IsTimerAtivo)
            {
                // Calcular tempo decorrido desde o início da sessão atual
                TimeSpan tempoDecorrido = DateTime.Now - _inicioEstudo;
                
                // Adicionar ao tempo acumulado
                TempoEstudo = _tempoAcumulado + tempoDecorrido;
                
                // DispatcherTimer já executa na thread UI
                DuracaoTexto = TempoEstudo.ToString(@"hh\:mm\:ss");
            }
        }

        private void IniciarTimer()
        {
            if (!IsTimerAtivo)
            {
                // Se não há duração carregada, começar do zero
                if (string.IsNullOrWhiteSpace(DuracaoTexto) || DuracaoTexto == "00:00:00")
                {
                    _tempoAcumulado = TimeSpan.Zero;
                }
                else
                {
                    // Carregar duração existente como tempo acumulado
                    if (TimeSpan.TryParseExact(DuracaoTexto, @"hh\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture, out var duracao))
                    {
                        _tempoAcumulado = duracao;
                    }
                }

                _inicioEstudo = DateTime.Now;
                IsTimerAtivo = true;
                _timer.Start();
            }
        }

        private void PausarTimer()
        {
            if (IsTimerAtivo)
            {
                // Salvar o tempo acumulado antes de pausar
                TimeSpan tempoDecorrido = DateTime.Now - _inicioEstudo;
                _tempoAcumulado = _tempoAcumulado + tempoDecorrido;
                
                IsTimerAtivo = false;
                _timer.Stop();
            }
        }

        private void ReiniciarTimer()
        {
            _timer.Stop();
            IsTimerAtivo = false;
            _tempoAcumulado = TimeSpan.Zero;
            TempoEstudo = TimeSpan.Zero;
            DuracaoTexto = "00:00:00";
        }

        private void AlternarTimer()
        {
            if (IsTimerAtivo)
            {
                PausarTimer();
            }
            else
            {
                IniciarTimer();
            }
        }

        private void AlternarTimerEMinimizar()
        {
            // Guardar estado anterior antes de alternar
            bool timerEstaRodando = IsTimerAtivo;

            // Alternar o timer
            AlternarTimer();

            // Se o timer ESTAVA PARADO e AGORA ESTÁ RODANDO (foi iniciado)
            // E está marcado para minimizar, solicitar minimização
            if (!timerEstaRodando && IsTimerAtivo && MinimizarParaBandeja)
            {
                OnSolicitarMinimizacao?.Invoke();
            }

            // Se o timer ESTAVA RODANDO e AGORA ESTÁ PARADO (foi pausado)
            // E está marcado para minimizar, restaurar a janela
            if (timerEstaRodando && !IsTimerAtivo && MinimizarParaBandeja)
            {
                OnSolicitarRestauracao?.Invoke();
            }
        }

        /// <summary>
        /// <summary>
        /// Inicializa o ViewModel para modo revisão com disciplina, assunto e tipo de estudo pré-selecionados.
        /// 
        /// FLUXO DE REVISÃO COMPLETO:
        /// ────────────────────────────
        /// 1. Usuário clica em revisão pendente (RevisaoId) na lista
        /// 2. InicializarModoRevisaoAsync() é chamado com revisaoId (ex: 42)
        /// 3. RevisaoId = 42 é armazenado nesta propriedade (abaixo)
        /// 4. Usuário edita o estudo e clica em "Salvar"
        /// 5. SalvarAsync() cria novo Estudo (ex: Id 999)
        /// 6. EstudoTransactionService marca revisão 42:
        ///    └─ Revisao.EstudoRealizadoId = 999
        /// 7. Revisão fica concluída e sai da lista de pendentes
        /// 
        /// IMPORTANTE: O EstudoRealizadoId é preenchido durante a transação de salva
        /// (EstudoTransactionService.SalvarEstudoComRevisoeseAssuntoAsync),
        /// não aqui. Este método apenas armazena o ID da revisão para referência futura.
        /// 
        /// Veja também: SalvarAsync() - linha ~636
        /// </summary>
        public async Task InicializarModoRevisaoAsync(Disciplina disciplina, Assunto assunto, TipoEstudo tipoEstudo, int revisaoId)
        {
            try
            {
                IsLoading = true;

                // Carregar dados necessários
                await CarregarDadosAsync();

                // Todas as atribuições de propriedades devem ser feitas na thread UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Preencher a coleção de assuntos com os assuntos da disciplina selecionada
                    Assuntos.Clear();
                    foreach (var a in TodosAssuntos.Where(x => x.DisciplinaId == disciplina.Id))
                    {
                        Assuntos.Add(a);
                    }

                    // Definir modo revisão
                    IsRevisao = true;
                    DisciplinaRevisao = disciplina;
                    AssuntoRevisao = assunto;
                    TipoEstudoRevisao = tipoEstudo;
                    
                    // ✅ CRÍTICO: RevisaoId é armazenado aqui!
                    // Será usado em SalvarAsync() para marcar a revisão original como concluída
                    // com o novo EstudoRealizadoId (do estudo que está sendo criado)
                    RevisaoId = revisaoId;

                    // Pré-selecionar os valores nos comboboxes
                    // IMPORTANTE: Usar as mesmas instâncias que estão nas coleções
                    DisciplinaSelecionada = Disciplinas.FirstOrDefault(d => d.Id == disciplina.Id);
                    AssuntoSelecionado = Assuntos.FirstOrDefault(a => a.Id == assunto.Id);
                    TipoEstudoSelecionado = TiposEstudo.FirstOrDefault(t => t.Id == tipoEstudo.Id);

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Seleção final:");
                    System.Diagnostics.Debug.WriteLine($"  - DisciplinaSelecionada: {DisciplinaSelecionada?.Nome} (ID: {DisciplinaSelecionada?.Id})");
                    System.Diagnostics.Debug.WriteLine($"  - AssuntoSelecionado: {AssuntoSelecionado?.Nome} (ID: {AssuntoSelecionado?.Id})");
                    System.Diagnostics.Debug.WriteLine($"  - TipoEstudoSelecionado: {TipoEstudoSelecionado?.Nome} (ID: {TipoEstudoSelecionado?.Id})");
                    System.Diagnostics.Debug.WriteLine($"  - DisciplinaSelecionada == disciplina: {ReferenceEquals(DisciplinaSelecionada, disciplina)}");
                    System.Diagnostics.Debug.WriteLine($"  - AssuntoSelecionado == assunto: {ReferenceEquals(AssuntoSelecionado, assunto)}");
                    System.Diagnostics.Debug.WriteLine($"  - TipoEstudoSelecionado == tipoEstudo: {ReferenceEquals(TipoEstudoSelecionado, tipoEstudo)}");

                    // Definir data como hoje
                    DataEstudo = DateTime.Now.Date;

                    // Definir título
                    Title = $"Revisão - {assunto.Nome}";

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Modo revisão inicializado:");
                    System.Diagnostics.Debug.WriteLine($"  - Disciplina: {DisciplinaSelecionada?.Nome}");
                    System.Diagnostics.Debug.WriteLine($"  - Assunto: {AssuntoSelecionado?.Nome}");
                    System.Diagnostics.Debug.WriteLine($"  - TipoEstudo: {TipoEstudoSelecionado?.Nome}");
                    System.Diagnostics.Debug.WriteLine($"  - IsRevisao: {IsRevisao}");
                });
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        public async Task InicializarComAssuntoAsync(Assunto assunto, int? pagina, int duracaoMinutos)
        {
            try
            {
                IsLoading = true;
                await CarregarDadosAsync(); // Garante que os dropdowns estão carregados

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Pré-selecionar disciplina
                    DisciplinaSelecionada = Disciplinas.FirstOrDefault(d => d.Id == assunto.DisciplinaId);
                    
                    // A seleção da disciplina acima vai filtrar a lista de Assuntos
                    // Agora, selecionar o assunto
                    AssuntoSelecionado = Assuntos.FirstOrDefault(a => a.Id == assunto.Id);

                    // Preencher duração e página
                    DuracaoTexto = TimeSpan.FromMinutes(duracaoMinutos).ToString(@"hh\:mm\:ss");
                    if(pagina.HasValue)
                    {
                        PaginaInicial = pagina.Value + 1;
                    }

                    // Definir título
                    //Title = $"Novo Estudo (Ciclo) - {assunto.Nome}";
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CarregarDadosAsync()
        {
            try
            {
                IsLoading = true;

                System.Diagnostics.Debug.WriteLine("[DEBUG] Iniciando carregamento de dados...");

                var tiposTask = _tipoEstudoService.ObterAtivosAsync();
                var disciplinasTask = _disciplinaService.ObterTodasAsync();
                var assuntosTask = _assuntoService.ObterTodosAsync();

                await Task.WhenAll(tiposTask, disciplinasTask, assuntosTask);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Dados carregados:");
                System.Diagnostics.Debug.WriteLine($"  - TiposEstudo: {tiposTask.Result.Count}");
                System.Diagnostics.Debug.WriteLine($"  - Disciplinas: {disciplinasTask.Result.Count}");
                System.Diagnostics.Debug.WriteLine($"  - Assuntos: {assuntosTask.Result.Count}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TiposEstudo.Clear();
                    foreach (var tipo in tiposTask.Result)
                        TiposEstudo.Add(tipo);

                    Disciplinas.Clear();
                    foreach (var disciplina in disciplinasTask.Result)
                        Disciplinas.Add(disciplina);

                    TodosAssuntos.Clear();
                    foreach (var assunto in assuntosTask.Result)
                        TodosAssuntos.Add(assunto);

                    // Inicialmente, não mostrar assuntos até selecionar disciplina
                    Assuntos.Clear();

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Coleções preenchidas na UI:");
                    System.Diagnostics.Debug.WriteLine($"  - TiposEstudo.Count: {TiposEstudo.Count}");
                    System.Diagnostics.Debug.WriteLine($"  - Disciplinas.Count: {Disciplinas.Count}");
                    System.Diagnostics.Debug.WriteLine($"  - TodosAssuntos.Count: {TodosAssuntos.Count}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ERRO ao carregar dados: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar dados: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CarregarDadosEstudo(Estudo estudo)
        {
            DataEstudo = estudo.Data;
            DuracaoTexto = estudo.Duracao.ToString(@"hh\:mm\:ss");
            Acertos = estudo.Acertos;
            Erros = estudo.Erros;
            PaginaInicial = estudo.PaginaInicial;
            PaginaFinal = estudo.PaginaFinal;
            Material = estudo.Material;
            Professor = estudo.Professor;
            Topicos = estudo.Topicos;
            Comentarios = estudo.Comentarios;

            // Aguardar carregamento dos dados para selecionar
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // Pequeno delay para garantir que os dados foram carregados
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TipoEstudoSelecionado = TiposEstudo.FirstOrDefault(t => t.Id == estudo.TipoEstudoId);
                    
                    // Primeiro encontrar o assunto para determinar a disciplina
                    var assunto = TodosAssuntos.FirstOrDefault(a => a.Id == estudo.AssuntoId);
                    if (assunto != null)
                    {
                        // Selecionar a disciplina do assunto
                        DisciplinaSelecionada = Disciplinas.FirstOrDefault(d => d.Id == assunto.DisciplinaId);
                        
                        // Filtrar assuntos da disciplina
                        FiltrarAssuntosPorDisciplina();
                        
                        // Selecionar o assunto
                        AssuntoSelecionado = Assuntos.FirstOrDefault(a => a.Id == estudo.AssuntoId);
                    }
                });
            });

            AtualizarEstatisticas();
        }

        partial void OnDisciplinaSelecionadaChanged(Disciplina? value)
        {
            FiltrarAssuntosPorDisciplina();
            // Limpar seleção de assunto quando disciplina muda
            AssuntoSelecionado = null;
        }

        private void FiltrarAssuntosPorDisciplina()
        {
            Assuntos.Clear();
            
            if (DisciplinaSelecionada != null)
            {
                var assuntosFiltrados = TodosAssuntos.Where(a => a.DisciplinaId == DisciplinaSelecionada.Id);
                foreach (var assunto in assuntosFiltrados)
                {
                    Assuntos.Add(assunto);
                }
            }
        }

        private async Task SalvarAsync()
        {
            if (!ValidarCampos())
                return;

            try
            {
                IsSaving = true;

                // Renderizar ícone de loading antes de iniciar operações assíncronas
                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ SalvarAsync() - Dispatcher.BeginInvoke executado, UI renderizada");
                    tcs.SetResult(true);
                }), System.Windows.Threading.DispatcherPriority.Render);
                await tcs.Task;

                var estudo = CriarEstudo();
                bool isNovoEstudo = _estudoOriginal == null;

                // Preparar dados para transação
                var revisoesParaCriar = new List<Revisao>();
                int? revisaoIdParaMarcarConcluida = null;
                bool? novoEstadoConcluido = null;

                if (isNovoEstudo)
                {
                    // Preparar revisões agendadas
                    // Se TipoRevisaoSelecionado for válido (não-nulo e com tipo de revisão), criar revisões
                    if (TipoRevisaoSelecionado?.TipoRevisao.HasValue == true)
                    {
                        revisoesParaCriar = CriarRevisoesAgendadas(TipoRevisaoSelecionado.TipoRevisao.Value);
                    }

                    // Preparar marcação de revisão como concluída
                    // ────────────────────────────────────────
                    // Quando em modo revisão, marca a revisão ORIGINAL como concluída
                    // com EstudoRealizadoId = novo estudo que foi criado nesta transação
                    // 
                    // Fluxo:
                    // RevisaoId (ex: 42) armazenado em InicializarModoRevisaoAsync
                    //   ↓
                    // SalvarAsync() cria novo Estudo (ex: Id 999)
                    //   ↓
                    // EstudoTransactionService recebe revisaoIdParaMarcarConcluida = 42
                    //   ↓
                    // Service marca revisão 42: EstudoRealizadoId = 999
                    //   ↓
                    // Revisão sai da lista de pendentes (possui EstudoRealizadoId)
                    if (IsRevisao && RevisaoId.HasValue)
                    {
                        revisaoIdParaMarcarConcluida = RevisaoId.Value;
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Fluxo Revisão:");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG]   └─ Revisão ID {RevisaoId.Value} será concluída");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG]   └─ EstudoRealizadoId será definido como: {estudo.Id}");
                    }

                    // Preparar atualização de assunto
                    if (AssuntoSelecionado != null)
                    {
                        bool estadoAnterior = AssuntoSelecionado.Concluido;
                        if (estadoAnterior != MarcarAssuntoConcluido)
                        {
                            novoEstadoConcluido = MarcarAssuntoConcluido;
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Atualizando assunto '{AssuntoSelecionado.Nome}' - Concluído: {MarcarAssuntoConcluido}");
                        }
                    }
                }

                // Executar transação única
                await _transactionService.SalvarEstudoComRevisoeseAssuntoAsync(
                    estudo,
                    isNovoEstudo,
                    AssuntoSelecionado,
                    novoEstadoConcluido,
                    revisoesParaCriar,
                    revisaoIdParaMarcarConcluida);

                // Preparar mensagens de feedback
                string mensagemRevisao = "";
                if (isNovoEstudo && TipoRevisaoSelecionado?.TipoRevisao.HasValue == true)
                {
                    if (TipoRevisaoSelecionado.TipoRevisao == TipoRevisaoEnum.Classico24h)
                    {
                        mensagemRevisao = "\n📅 Revisões agendadas: 24h, 7d e 30 dias";
                    }
                    else if (TipoRevisaoSelecionado.TipoRevisao == TipoRevisaoEnum.Ciclo42)
                    {
                        mensagemRevisao = "\n📅 Revisão agendada: Método 4.2";
                    }
                }

                string mensagemAssunto = (isNovoEstudo && MarcarAssuntoConcluido) ? "\n✅ Assunto marcado como concluído!" : "";

                if (isNovoEstudo)
                {
                    _notificationService.ShowSuccess(
                        "Sucesso",
                        $"Estudo registrado com sucesso!\n\n" +
                        $"📚 Assunto: {AssuntoSelecionado?.Nome}\n" +
                        $"⏱️ Duração: {DuracaoTexto}\n" +
                        $"📊 Questões: {TotalQuestoes} ({RendimentoPercentual:F1}% de acerto)" +
                        (IsRevisao ? "\n✅ Revisão marcada como concluída!" : "") +
                        mensagemAssunto +
                        mensagemRevisao);
                }
                else
                {
                    _notificationService.ShowSuccess(
                        "Sucesso",
                        $"Estudo atualizado com sucesso!\n\n" +
                        $"📚 Assunto: {AssuntoSelecionado?.Nome}\n" +
                        $"⏱️ Duração: {DuracaoTexto}\n" +
                        $"📊 Questões: {TotalQuestoes} ({RendimentoPercentual:F1}% de acerto)");
                }

                // Disparar evento de estudo salvo
                EstudoSalvo?.Invoke(this, EventArgs.Empty);

                // Resetar alterações pendentes antes de navegar
                // Isso evita que o EditableViewBehavior exiba confirmação após salvamento bem-sucedido
                HasUnsavedChanges = false;

                // Navegar de volta para a lista de estudos
                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError(
                    "Erro ao Salvar",
                    $"Erro ao salvar estudo: {ex.Message}\n\n" +
                    "Verifique se todos os campos estão preenchidos corretamente e tente novamente.");

                System.Diagnostics.Debug.WriteLine($"Erro ao salvar estudo: {ex}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void Cancelar()
        {
            // A verificação de alterações não salvas é feita em OnViewUnloadingAsync()
            // através do NavigationService.GoBack()
            // Navegar de volta para a lista de estudos
            _navigationService.GoBack();
        }

        private bool TemAlteracoesPendentes()
        {
            if (_estudoOriginal == null)
            {
                // Novo estudo - verificar se algum campo foi preenchido
                return TipoEstudoSelecionado != null ||
                       AssuntoSelecionado != null ||
                       DuracaoTexto != null ||
                       DataEstudo.Date != DateTime.Now.Date ||
                       Acertos > 0 ||
                       Erros > 0 ||
                       PaginaInicial > 0 ||
                       PaginaFinal > 0 ||
                       !string.IsNullOrWhiteSpace(Material) ||
                       !string.IsNullOrWhiteSpace(Professor) ||
                       !string.IsNullOrWhiteSpace(Topicos) ||
                       !string.IsNullOrWhiteSpace(Comentarios);
            }
            else
            {
                // Estudo existente - verificar se houve alterações
                return TipoEstudoSelecionado?.Id != _estudoOriginal.TipoEstudoId ||
                       AssuntoSelecionado?.Id != _estudoOriginal.AssuntoId ||
                       DataEstudo.Date != _estudoOriginal.Data.Date ||
                       DuracaoTexto != _estudoOriginal.Duracao.ToString(@"hh\:mm\:ss") ||
                       Acertos != _estudoOriginal.Acertos ||
                       Erros != _estudoOriginal.Erros ||
                       PaginaInicial != _estudoOriginal.PaginaInicial ||
                       PaginaFinal != _estudoOriginal.PaginaFinal ||
                       Material?.Trim() != _estudoOriginal.Material?.Trim() ||
                       Professor?.Trim() != _estudoOriginal.Professor?.Trim() ||
                       Topicos?.Trim() != _estudoOriginal.Topicos?.Trim() ||
                       Comentarios?.Trim() != _estudoOriginal.Comentarios?.Trim();
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
                "Você tem alterações não salvas. Deseja descartá-las?");

            return resultado != ToastMessageBoxResult.Yes;
        }

        private bool ValidarCampos()
        {
            var erros = new List<string>();

            // Validação de campos obrigatórios
            if (TipoEstudoSelecionado == null)
                erros.Add("• Selecione um tipo de estudo");

            if (DisciplinaSelecionada == null)
                erros.Add("• Selecione uma disciplina");

            if (AssuntoSelecionado == null)
                erros.Add("• Selecione um assunto");

            // Validação de duração
            if (string.IsNullOrWhiteSpace(DuracaoTexto))
            {
                erros.Add("• Informe a duração do estudo");
            }
            else if (!TimeSpan.TryParseExact(DuracaoTexto, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var duracao))
            {
                erros.Add("• Duração deve estar no formato hh:mm:ss (ex: 01:30:00)");
            }
            else if (duracao.TotalMinutes < 1)
            {
                erros.Add("• Duração mínima é de 1 minuto");
            }
            else if (duracao.TotalHours > 24)
            {
                erros.Add("• Duração máxima é de 24 horas");
            }

            // Validação de data
            if (DataEstudo > DateTime.Now.Date.AddDays(1))
            {
                erros.Add("• Data do estudo não pode ser no futuro");
            }

            // Validação de páginas
            if (PaginaInicial < 0 || PaginaFinal < 0)
            {
                erros.Add("• Páginas devem ser números positivos");
            }
            else if (PaginaFinal > 0 && PaginaInicial > 0 && PaginaFinal < PaginaInicial)
            {
                erros.Add("• Página final deve ser maior ou igual à página inicial");
            }

            // Validação de questões
            if (Acertos < 0 || Erros < 0)
            {
                erros.Add("• Número de acertos e erros deve ser positivo");
            }

            // Validação de campos de texto muito longos
            if (!string.IsNullOrEmpty(Material) && Material.Length > 200)
                erros.Add("• Material não pode ter mais de 200 caracteres");

            if (!string.IsNullOrEmpty(Professor) && Professor.Length > 100)
                erros.Add("• Nome do professor não pode ter mais de 100 caracteres");

            if (!string.IsNullOrEmpty(Topicos) && Topicos.Length > 1000)
                erros.Add("• Tópicos não pode ter mais de 1000 caracteres");

            if (!string.IsNullOrEmpty(Comentarios) && Comentarios.Length > 2000)
                erros.Add("• Comentários não pode ter mais de 2000 caracteres");

            // Exibir erros se houver
            if (erros.Count > 0)
            {
                var mensagem = "Por favor, corrija os seguintes problemas:\n\n" + string.Join("\n", erros);
                _notificationService.ShowWarning("Validação", mensagem);
                return false;
            }

            return true;
        }

        private Estudo CriarEstudo()
        {
            TimeSpan.TryParseExact(DuracaoTexto, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var duracao);

            return new Estudo
            {
                Id = _estudoOriginal?.Id ?? 0,
                TipoEstudoId = TipoEstudoSelecionado!.Id,
                AssuntoId = AssuntoSelecionado!.Id,
                Data = DataEstudo,
                DuracaoTicks = duracao.Ticks,
                Acertos = Acertos,
                Erros = Erros,
                PaginaInicial = PaginaInicial,
                PaginaFinal = PaginaFinal,
                Material = string.IsNullOrWhiteSpace(Material) ? null : Material.Trim(),
                Professor = string.IsNullOrWhiteSpace(Professor) ? null : Professor.Trim(),
                Topicos = string.IsNullOrWhiteSpace(Topicos) ? null : Topicos.Trim(),
                Comentarios = string.IsNullOrWhiteSpace(Comentarios) ? null : Comentarios.Trim()
            };
        }

        private void AtualizarEstatisticas()
        {
            TotalQuestoes = Acertos + Erros;
            RendimentoPercentual = TotalQuestoes > 0 ? Math.Round((double)Acertos / TotalQuestoes * 100, 1) : 0;
            TotalPaginas = Math.Max(0, PaginaFinal - PaginaInicial + 1);
            MostrarEstatisticas = TotalQuestoes > 0 || TotalPaginas > 1;

            // Calcular métricas de ritmo
            CalcularMetricasRitmo();
        }

        /// <summary>
        /// Calcula as métricas de ritmo de estudo baseado no tempo decorrido
        /// 
        /// Fórmulas:
        /// - Ritmo de Páginas: Total de páginas lidas / Duração em horas
        /// - Páginas por Minuto: Total de páginas lidas / Duração em minutos
        /// - Ritmo de Questões: Total de questões respondidas / Duração em horas
        /// - Questões por Minuto: Total de questões respondidas / Duração em minutos
        /// </summary>
        private void CalcularMetricasRitmo()
        {
            // Converter duração de texto para TimeSpan
            if (!TimeSpan.TryParseExact(DuracaoTexto, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var duracao))
            {
                // Se não conseguir fazer parse, zerar as métricas
                RitmoPaginas = 0;
                PaginasPorMinuto = 0;
                RitmoQuestoes = 0;
                QuestoesPorMinuto = 0;
                return;
            }

            // Se a duração for zero, não calcular (evitar divisão por zero)
            if (duracao.TotalMinutes == 0)
            {
                RitmoPaginas = 0;
                PaginasPorMinuto = 0;
                RitmoQuestoes = 0;
                QuestoesPorMinuto = 0;
                return;
            }

            // Calcular Ritmo de Páginas (páginas por minuto)
            RitmoPaginas = TotalPaginas > 0 
                ? (int)Math.Round(duracao.TotalMinutes / TotalPaginas, 0) 
                : 0;

            // Calcular Páginas por Minuto
            PaginasPorMinuto = TotalPaginas > 0 
                ? Math.Round(TotalPaginas / duracao.TotalMinutes, 2) 
                : 0;

            // Calcular Ritmo de Questões (questões por minuto)
            RitmoQuestoes = TotalQuestoes > 0 
                ? (int)Math.Round(duracao.TotalMinutes / TotalQuestoes, 0) 
                : 0;

            // Calcular Questões por Minuto
            QuestoesPorMinuto = TotalQuestoes > 0 
                ? Math.Round(TotalQuestoes / duracao.TotalMinutes, 2) 
                : 0;
        }

        partial void OnAcertosChanged(int value)
        {
            AtualizarEstatisticas();
            VerificarAlteracoes();
        }

        partial void OnErrosChanged(int value)
        {
            AtualizarEstatisticas();
            VerificarAlteracoes();
        }

        partial void OnPaginaInicialChanged(int value)
        {
            AtualizarEstatisticas();
            VerificarAlteracoes();
        }

        partial void OnPaginaFinalChanged(int value)
        {
            AtualizarEstatisticas();
            VerificarAlteracoes();
        }

        partial void OnTipoEstudoSelecionadoChanged(TipoEstudo? value)
        {
            VerificarAlteracoes();
        }

        partial void OnAssuntoSelecionadoChanged(Assunto? value)
        {
            // Atualizar checkbox "Assunto Concluído" baseado no estado do assunto selecionado
            if (value != null)
            {
                MarcarAssuntoConcluido = value.Concluido;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Assunto '{value.Nome}' selecionado. Concluído: {value.Concluido}");
            }
            else
            {
                MarcarAssuntoConcluido = false;
            }
            
            VerificarAlteracoes();
        }

        partial void OnDataEstudoChanged(DateTime value)
        {
            VerificarAlteracoes();
        }

        partial void OnDuracaoTextoChanged(string value)
        {
            // Recalcular métricas de ritmo quando a duração mudar
            CalcularMetricasRitmo();
            VerificarAlteracoes();
        }

        partial void OnMaterialChanged(string? value)
        {
            VerificarAlteracoes();
        }

        partial void OnProfessorChanged(string? value)
        {
            VerificarAlteracoes();
        }

        partial void OnTopicosChanged(string? value)
        {
            VerificarAlteracoes();
        }

        partial void OnComentariosChanged(string? value)
        {
            VerificarAlteracoes();
        }

        private void VerificarAlteracoes()
        {
            HasUnsavedChanges = TemAlteracoesPendentes();
        }

        /// <summary>
        /// Salva um rascunho temporário dos dados (funcionalidade futura)
        /// </summary>
        private Task SalvarRascunhoAsync()
        {
            try
            {
                // Implementação futura: salvar em cache local ou banco temporário
                // Por enquanto, apenas mostra uma mensagem
                _notificationService.ShowInfo(
                    "Rascunho",
                    "Funcionalidade de rascunho será implementada em versão futura.\n\n" +
                    "Por enquanto, use o botão 'Salvar' para salvar definitivamente o estudo.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar rascunho: {ex}");
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Inicializa as opções de revisão para o ComboBox
        /// </summary>
        private void InicializarTiposRevisao()
        {
            TiposRevisao.Clear();
            
            // Opção em branco (não agendar revisão)
            TiposRevisao.Add(new TipoRevisaoOpcao 
            { 
                Nome = "", 
                Descricao = "",
                TipoRevisao = null 
            });
            
            // Opção clássica
            TiposRevisao.Add(new TipoRevisaoOpcao 
            { 
                Nome = "Método Clássico (24h, 7d, 30d)", 
                Descricao = "O método Clássico (24/7/30) é ideal para quem é extremamente organizado e tem um edital longo pela frente.\n\nComo funciona:\n\n24h: No dia seguinte ao estudo, faça uma revisão rápida (10-15 min) do que estudou ontem.\n7 dias: Uma semana depois, revise novamente.\n30 dias: Um mês depois, faça a terceira revisão.\n\nVantagem: Garante a retenção de longo prazo.\n\nDesvantagem: Cria uma \"bola de neve\". Em alguns meses, você pode passar mais tempo revisando do que avançando em matérias novas.",
                TipoRevisao = TipoRevisaoEnum.Classico24h 
            });
            
            // Opção 4.2
            TiposRevisao.Add(new TipoRevisaoOpcao 
            { 
                Nome = "Método 4.2", 
                Descricao = "O método 4.2 é muito popular atualmente por ser mais simples de gerenciar e evitar a \"bola de neve\".\n\nComo funciona:\n\n4 dias você estuda teoria nova (ex: Seg, Ter, Qua, Qui).\n2 dias você apenas revisa o que estudou nesses 4 dias, exclusivamente através de questões (ex: Sex, Sáb).\n1 dia você descansa ou faz simulados.\n\nVantagem: Muito dinâmico e focado em resolução de exercícios.\n\nDesvantagem: Exige disciplina para não pular os dias de revisão.",
                TipoRevisao = TipoRevisaoEnum.Ciclo42 
            });
        }

        /// <summary>
        /// Obtém os dias da semana configurados para revisão 4.2
        /// </summary>
        private List<DayOfWeek> ObterDiasRevisao42()
        {
            var dias = new List<DayOfWeek>();
            
            if (_configurationService?.Settings?.Study != null)
            {
                if (_configurationService.Settings.Study.Method42MondayEnabled)
                    dias.Add(DayOfWeek.Monday);
                if (_configurationService.Settings.Study.Method42TuesdayEnabled)
                    dias.Add(DayOfWeek.Tuesday);
                if (_configurationService.Settings.Study.Method42WednesdayEnabled)
                    dias.Add(DayOfWeek.Wednesday);
                if (_configurationService.Settings.Study.Method42ThursdayEnabled)
                    dias.Add(DayOfWeek.Thursday);
                if (_configurationService.Settings.Study.Method42FridayEnabled)
                    dias.Add(DayOfWeek.Friday);
                if (_configurationService.Settings.Study.Method42SaturdayEnabled)
                    dias.Add(DayOfWeek.Saturday);
                if (_configurationService.Settings.Study.Method42SundayEnabled)
                    dias.Add(DayOfWeek.Sunday);
            }
            
            // Se nenhum dia foi configurado, usar padrão (sábado e domingo)
            if (dias.Count == 0)
            {
                dias.Add(DayOfWeek.Saturday);
                dias.Add(DayOfWeek.Sunday);
            }
            
            return dias;
        }

        /// <summary>
        /// Encontra o próximo dia da semana especificado a partir da data fornecida
        /// </summary>
        private DateTime EncontrarProximoDia(DateTime dataInicio, DayOfWeek diaBuscado)
        {
            var data = dataInicio.AddDays(1); // Começar a partir do próximo dia
            while (data.DayOfWeek != diaBuscado)
            {
                data = data.AddDays(1);
            }
            return data;
        }

        /// <summary>
        /// Obtém a última data de revisão 4.2 (ou data anterior se nenhuma revisão existir)
        /// </summary>
        private async Task<DateTime> ObterUltimaDataRevisao42Async()
        {
            try
            {
                var diasRevisao = ObterDiasRevisao42();
                if (diasRevisao.Count < 2)
                {
                    diasRevisao = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };
                }

                // Encontrar o último dia de revisão (maior DayOfWeek value)
                var ultimoDiaRevisao = diasRevisao.OrderByDescending(d => d).First();
                
                // Encontrar a última ocorrência deste dia
                var hoje = DateTime.Now.Date;
                var data = hoje;
                
                // Se hoje é o dia de revisão, voltar uma semana
                if (data.DayOfWeek == ultimoDiaRevisao)
                {
                    return data;
                }
                
                // Voltar até encontrar o dia de revisão
                while (data.DayOfWeek != ultimoDiaRevisao)
                {
                    data = data.AddDays(-1);
                }
                
                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro ao obter última data de revisão: {ex.Message}");
                return DateTime.Now.Date.AddDays(-1);
            }
        }

        /// <summary>
        /// Obtém o dia de revisão 4.2 baseado na posição do estudo na semana
        /// Considera todos os estudos desde a última revisão até o dia atual
        /// Estudos em posição ímpar (1, 3, 5...) vão para o primeiro dia
        /// Estudos em posição par (2, 4, 6...) vão para o segundo dia
        /// </summary>
        private async Task<DateTime> ObterDiaRevisao42ComDistribuicaoAsync(int estudoId, DateTime dataEstudo)
        {
            try
            {
                // Obter os dias configurados para revisão 4.2
                var diasRevisao = ObterDiasRevisao42();
                
                if (diasRevisao.Count < 2)
                {
                    diasRevisao = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };
                }
                
                // Encontrar os próximos dois dias de revisão
                var dia1 = EncontrarProximoDia(dataEstudo, diasRevisao[0]);
                var dia2 = EncontrarProximoDia(dataEstudo, diasRevisao[1]);
                
                // Ordenar para garantir que dia1 < dia2
                if (dia1 > dia2)
                {
                    var temp = dia1;
                    dia1 = dia2;
                    dia2 = temp;
                }
                
                // Obter a última data de revisão
                var ultimaDataRevisao = await ObterUltimaDataRevisao42Async();
                
                // Obter todos os estudos desde a última revisão até o dia atual
                var estudosSemana = await _estudoService.ObterPorIntervaloDataAsync(
                    ultimaDataRevisao.AddDays(1), 
                    dataEstudo);
                
                // Encontrar a posição do estudo atual na semana
                var posicaoEstudo = 0;
                foreach (var estudo in estudosSemana)
                {
                    posicaoEstudo++;
                    if (estudo.Id == estudoId)
                    {
                        break;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Estudo {estudoId} na posição {posicaoEstudo} da semana (desde {ultimaDataRevisao.AddDays(1):yyyy-MM-dd})");
                
                // Distribuir alternadamente
                if (posicaoEstudo % 2 == 1) // Posição ímpar
                {
                    return dia1;
                }
                else // Posição par
                {
                    return dia2;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro ao obter dia de revisão 4.2: {ex.Message}");
                // Fallback para sábado
                return EncontrarProximoDia(dataEstudo, DayOfWeek.Saturday);
            }
        }

        /// <summary>
        /// Cria as revisões agendadas com base no tipo selecionado (sem salvar no banco)
        /// Retorna uma lista de revisões para serem salvas em transação
        /// </summary>
        private List<Revisao> CriarRevisoesAgendadas(TipoRevisaoEnum tipoRevisao)
        {
            var revisoes = new List<Revisao>();

            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Criando revisões agendadas, tipo: {tipoRevisao}");

                if (tipoRevisao == TipoRevisaoEnum.Classico24h)
                {
                    // Criar 6 revisões: 24h, 7d, 30d, 90d, 120d, 180d
                    // Sequência estendida de Ebbinghaus para retenção de longo prazo
                    revisoes.Add(CriarRevisao(TipoRevisaoEnum.Classico24h, DataEstudo.AddDays(1)));
                    revisoes.Add(CriarRevisao(TipoRevisaoEnum.Classico7d, DataEstudo.AddDays(7)));
                    revisoes.Add(CriarRevisao(TipoRevisaoEnum.Classico30d, DataEstudo.AddDays(30)));
                    revisoes.Add(CriarRevisao(TipoRevisaoEnum.Classico90d, DataEstudo.AddDays(90)));
                    revisoes.Add(CriarRevisao(TipoRevisaoEnum.Classico120d, DataEstudo.AddDays(120)));
                    revisoes.Add(CriarRevisao(TipoRevisaoEnum.Classico180d, DataEstudo.AddDays(180)));

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Preparadas 6 revisões clássicas estendidas");
                }
                else if (tipoRevisao == TipoRevisaoEnum.Ciclo42)
                {
                    // Para o método 4.2, usar uma data padrão (será ajustada se necessário)
                    var diaRevisao = DataEstudo.AddDays(1);
                    revisoes.Add(CriarRevisao(TipoRevisaoEnum.Ciclo42, diaRevisao));

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Preparada revisão 4.2 para {diaRevisao:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Erro ao preparar revisões agendadas: {ex.Message}");
                _notificationService.ShowError(
                    "Erro ao Agendar",
                    $"Erro ao preparar revisões: {ex.Message}");
            }

            return revisoes;
        }

        /// <summary>
        /// Cria uma instância de Revisao sem salvar no banco
        /// </summary>
        private Revisao CriarRevisao(TipoRevisaoEnum tipo, DateTime dataProgramada)
        {
            return new Revisao
            {
                TipoRevisao = tipo,
                DataProgramadaTicks = dataProgramada.Ticks
            };
        }

        /// <summary>
        /// Cria as revisões agendadas com base no tipo selecionado (método legado - mantido para compatibilidade)
        /// </summary>
        private async Task CriarRevisoesAgendadasAsync(int estudoId, TipoRevisaoEnum tipoRevisao)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Criando revisões agendadas para estudo {estudoId}, tipo: {tipoRevisao}");

                if (tipoRevisao == TipoRevisaoEnum.Classico24h)
                {
                    // Criar 3 revisões: 24h, 7d, 30d
                    await CriarRevisaoAsync(estudoId, TipoRevisaoEnum.Classico24h, DataEstudo.AddDays(1));
                    await CriarRevisaoAsync(estudoId, TipoRevisaoEnum.Classico7d, DataEstudo.AddDays(7));
                    await CriarRevisaoAsync(estudoId, TipoRevisaoEnum.Classico30d, DataEstudo.AddDays(30));
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Criadas 3 revisões clássicas para estudo {estudoId}");
                }
                else if (tipoRevisao == TipoRevisaoEnum.Ciclo42)
                {
                    // Obter o dia de revisão com distribuição alternada
                    var diaRevisao = await ObterDiaRevisao42ComDistribuicaoAsync(estudoId, DataEstudo);
                    
                    // Criar 1 revisão no dia calculado
                    await CriarRevisaoAsync(estudoId, TipoRevisaoEnum.Ciclo42, diaRevisao);
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Criada revisão 4.2 para estudo {estudoId} em {diaRevisao:yyyy-MM-dd} ({diaRevisao:dddd})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Erro ao criar revisões agendadas: {ex.Message}");
                _notificationService.ShowError(
                    "Erro ao Agendar",
                    $"Erro ao agendar revisões: {ex.Message}");
            }
        }

        /// <summary>
        /// Cria uma única revisão no banco de dados usando o RevisaoService
        /// </summary>
        private async Task CriarRevisaoAsync(int estudoId, TipoRevisaoEnum tipo, DateTime dataProgramada)
        {
            bool sucesso = await _revisaoService.CriarRevisaoAsync(estudoId, tipo, dataProgramada);
            
            if (!sucesso)
            {
                throw new Exception($"Falha ao criar revisão do tipo {tipo} para o estudo {estudoId}");
            }
        }

        /// <summary>
        /// Abre o link do caderno de questões do assunto selecionado no navegador padrão
        /// </summary>
        private void AbrirCadernoQuestoes()
        {
            try
            {
                if (AssuntoSelecionado?.CadernoQuestoes != null && 
                    !string.IsNullOrWhiteSpace(AssuntoSelecionado.CadernoQuestoes))
                {
                    // Validar se é uma URL válida
                    string url = AssuntoSelecionado.CadernoQuestoes;
                    
                    // Se não começar com http:// ou https://, adicionar https://
                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        url = "https://" + url;
                    }

                    // Abrir no navegador padrão
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Abrindo caderno de questões: {url}");
                }
                else
                {
                    _notificationService.ShowWarning(
                        "Caderno de Questões",
                        "O assunto selecionado não possui um caderno de questões cadastrado.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro ao abrir caderno de questões: {ex.Message}");
                _notificationService.ShowError(
                    "Erro ao Abrir Link",
                    $"Não foi possível abrir o caderno de questões: {ex.Message}");
            }
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
                    // DispatcherTimer não precisa ser descartado manualmente
                    _timer?.Stop();
                }
                _disposed = true;
            }
        }
    }

    // Classe auxiliar para opções de revisão no ComboBox
    public class TipoRevisaoOpcao
    {
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public TipoRevisaoEnum? TipoRevisao { get; set; }

        public override string ToString() => Nome;
    }
}
