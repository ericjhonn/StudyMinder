using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Navigation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;

namespace StudyMinder.ViewModels
{
    public enum EditalViewType
    {
        Informacoes,
        Assuntos,
        Cronograma
    }

    public partial class EditarEditalViewModel : BaseViewModel, IEditableViewModel
    {
        private readonly EditalService _editalService;
        private readonly NavigationService _navigationService;
        private readonly Edital? _editalOriginal;
        private readonly Data.StudyMinderContext _context;
        private readonly RevisaoNotificacaoService _revisaoNotificacaoService;
        private readonly INotificationService _notificationService;
        private bool _isSaving = false;
        private bool _isLoading = false;
        private string _searchText = string.Empty;
        private EditalViewType _currentView = EditalViewType.Informacoes;
        /// <summary>
        /// Dados COMPLETOS e INTACTOS dos assuntos vinculados - NUNCA deve ser filtrado
        /// Usado APENAS para salvamento no banco de dados
        /// </summary>
        private List<DisciplinaAssuntoGroup> _assuntosOriginaisIntactos = new();
        /// <summary>
        /// Dados dos assuntos para exibição - pode ser filtrado pela pesquisa
        /// </summary>
        private List<DisciplinaAssuntoGroup> _assuntosOriginais = new();
        private Dictionary<EditalCronograma, EditalCronograma> _cronogramaBackup = new(); // Backup para cancelamento
        private readonly System.Timers.Timer _searchTimer;
        private int _itensPorPaginaEdicao = 20;

        /// <summary>
        /// Dicionário para rastrear seleções de assuntos em modo de edição (persiste ao mudar de página)
        /// </summary>
        private Dictionary<int, bool> _selecoesPorAssuntoIdEdicao = new();

        /// <summary>
        /// Cache de edital para evitar recarregamentos desnecessários
        /// </summary>
        private Edital? _cacheEdital;

        /// <summary>
        /// Cópia do estado do edital após o último salvamento bem-sucedido
        /// Usado para verificar se há alterações não salvas
        /// </summary>
        private Edital? _editalSalvo;

        private IAsyncRelayCommand? _salvarAsyncCommand;
        private IRelayCommand? _cancelarCommand;
        private IRelayCommand? _adicionarAssuntosCommand;
        private IRelayCommand? _adicionarEventoCommand;
        private IRelayCommand<EditalCronograma>? _salvarEventoCommand;
        private IRelayCommand<EditalCronograma>? _cancelarEdicaoEventoCommand;
        private IRelayCommand<EditalCronograma>? _iniciarEdicaoEventoCommand;
        private IRelayCommand<EditalCronograma>? _removerEventoCommand;
        private IRelayCommand? _abrirLinkEditalCommand;

        public IAsyncRelayCommand SalvarAsyncCommand => _salvarAsyncCommand ??= new AsyncRelayCommand(SalvarAsync);
        public IRelayCommand CancelarCommand => _cancelarCommand ??= new RelayCommand(Cancelar);
        public IRelayCommand AdicionarAssuntosCommand => _adicionarAssuntosCommand ??= new RelayCommand(AdicionarAssuntos);
        public IRelayCommand AdicionarEventoCommand => _adicionarEventoCommand ??= new RelayCommand(AdicionarEvento);
        public IRelayCommand<EditalCronograma> SalvarEventoCommand => _salvarEventoCommand ??= new RelayCommand<EditalCronograma>(SalvarEvento!);
        public IRelayCommand<EditalCronograma> CancelarEdicaoEventoCommand => _cancelarEdicaoEventoCommand ??= new RelayCommand<EditalCronograma>(CancelarEdicaoEvento!);
        public IRelayCommand<EditalCronograma> IniciarEdicaoEventoCommand => _iniciarEdicaoEventoCommand ??= new RelayCommand<EditalCronograma>(IniciarEdicaoEvento!);
        public IRelayCommand<EditalCronograma> RemoverEventoCommand => _removerEventoCommand ??= new RelayCommand<EditalCronograma>(RemoverEvento!);
        public IRelayCommand AbrirLinkEditalCommand => _abrirLinkEditalCommand ??= new RelayCommand(AbrirLinkEdital);

        public EditarEditalViewModel(EditalService editalService, NavigationService navigationService, Data.StudyMinderContext context, RevisaoNotificacaoService revisaoNotificacaoService, INotificationService notificationService, Edital? edital = null)
        {
            _editalService = editalService;
            _navigationService = navigationService;
            _context = context;
            _revisaoNotificacaoService = revisaoNotificacaoService;
            _notificationService = notificationService;
            _editalOriginal = edital;

            // Timer para debounce da pesquisa
            _searchTimer = new System.Timers.Timer(300); // 300ms de delay
            _searchTimer.Elapsed += SearchTimer_Elapsed;
            _searchTimer.AutoReset = false;

            if (edital != null)
            {
                // Modo de edição
                Id = edital.Id;
                Cargo = edital.Cargo;
                Orgao = edital.Orgao;
                Banca = edital.Banca;
                Area = edital.Area;
                Salario = edital.Salario;
                ValorInscricao = edital.ValorInscricao;
                VagasImediatas = edital.VagasImediatas;
                VagasCadastroReserva = edital.VagasCadastroReserva;
                Concorrencia = edital.Concorrencia;
                Colocacao = edital.Colocacao;
                NumeroInscricao = edital.NumeroInscricao;
                AcertosProva = edital.AcertosProva;
                ErrosProva = edital.ErrosProva;
                BrancosProva = edital.BrancosProva;
                AnuladasProva = edital.AnuladasProva;
                TipoProvaId = edital.TipoProvaId;
                Link = edital.Link;
                DataAbertura = edital.DataAbertura;
                DataProva = edital.DataProva;
                EscolaridadeId = edital.EscolaridadeId;
                FaseEditalId = edital.FaseEditalId;
                ProvaDiscursiva = edital.ProvaDiscursiva;
                ProvaTitulos = edital.ProvaTitulos;
                ProvaTaf = edital.ProvaTaf;
                ProvaPratica = edital.ProvaPratica;
                BoletoPago = edital.BoletoPago;
                IsArquivado = edital.Arquivado;
                Validade = edital.Validade;
                DataHomologacao = edital.DataHomologacao;
                Title = "Editar Edital";
            }
            else
            {
                // Modo de criação
                Title = "Novo Edital";
                IsArquivado = false;
            }

            // Inscrever nos eventos de notificação
            _revisaoNotificacaoService.RevisaoAdicionada += OnRevisaoAdicionada;
            _revisaoNotificacaoService.RevisaoAtualizada += OnRevisaoAtualizada;
            _revisaoNotificacaoService.RevisaoRemovida += OnRevisaoRemovida;

            CarregarDadosIniciais();
        }

        // Propriedades do Edital
        public int Id { get; set; }

        [ObservableProperty]
        private string _cargo = string.Empty;

        [ObservableProperty]
        private string _orgao = string.Empty;

        [ObservableProperty]
        private string? _banca;

        [ObservableProperty]
        private string? _area;

        [ObservableProperty]
        private string? _salario;

        [ObservableProperty]
        private string? _valorInscricao;

        [ObservableProperty]
        private int? _vagasImediatas;

        [ObservableProperty]
        private int? _vagasCadastroReserva;

        [ObservableProperty]
        private int? _concorrencia;

        [ObservableProperty]
        private int? _colocacao;

        /// <summary>
        /// Propriedade calculada que retorna o percentil da colocação.
        /// Indica quantos porcento da concorrência o usuário conseguiu superar.
        /// 100% = 1º lugar, 0% = último lugar
        /// </summary>
        public decimal? ColocacaoPercentil
        {
            get
            {
                if (!Colocacao.HasValue || !Concorrencia.HasValue)
                    return null;

                if (Concorrencia.Value == 0)
                    return null;

                // Fórmula: (Concorrência - Colocação) / Concorrência * 100
                decimal percentil = (decimal)(Concorrencia.Value - Colocacao.Value) / Concorrencia.Value * 100;
                return Math.Round(percentil, 2);
            }
        }

        [ObservableProperty]
        private string? _numeroInscricao;

        [ObservableProperty]
        private int? _acertosProva;

        [ObservableProperty]
        private int? _errosProva;

        [ObservableProperty]
        private int? _brancosProva;

        [ObservableProperty]
        private int? _anuladasProva;

        [ObservableProperty]
        private int? _tipoProvaId;

        [ObservableProperty]
        private string? _link;

        [ObservableProperty]
        private DateTime? _dataAbertura;

        [ObservableProperty]
        private DateTime? _dataProva;

        [ObservableProperty]
        private int? _escolaridadeId;

        [ObservableProperty]
        private int? _faseEditalId;

        [ObservableProperty]
        private bool _provaDiscursiva = false;

        [ObservableProperty]
        private bool _provaTitulos = false;

        [ObservableProperty]
        private bool _provaTaf = false;

        [ObservableProperty]
        private bool _provaPratica = false;

        [ObservableProperty]
        private bool _boletoPago = false;

        [ObservableProperty]
        private bool _isArquivado = false;

        [ObservableProperty]
        private int? _validade;

        [ObservableProperty]
        private DateTime? _dataHomologacao;

        public bool Encerrado
        {
            get => DataHomologacao.HasValue;
        }

        public DateTime? ValidadeFim
        {
            get
            {
                if (!DataHomologacao.HasValue || !Validade.HasValue)
                    return null;
                
                return DataHomologacao.Value.AddMonths(Validade.Value);
            }
        }

        // Propriedades de navegação
        public EditalViewType CurrentView
        {
            get => _currentView;
            set
            {
                if (SetProperty(ref _currentView, value))
                {
                    OnPropertyChanged(nameof(IsInformacoesView));
                    OnPropertyChanged(nameof(IsAssuntosView));
                    OnPropertyChanged(nameof(IsCronogramaView));

                    // CORREÇÃO: Limpar a pesquisa sempre que mudar de aba
                    SearchText = string.Empty;
                }
            }
        }

        public bool IsInformacoesView => CurrentView == EditalViewType.Informacoes;
        public bool IsAssuntosView => CurrentView == EditalViewType.Assuntos;
        public bool IsCronogramaView => CurrentView == EditalViewType.Cronograma;

        public new string Title { get; private set; } = "Edital";

        public Edital CurrentEdital
        {
            get
            {
                return new Edital
                {
                    Id = Id,
                    Cargo = Cargo,
                    Orgao = Orgao,
                    Banca = Banca,
                    Area = Area,
                    Salario = Salario,
                    ValorInscricao = ValorInscricao,
                    VagasImediatas = VagasImediatas,
                    VagasCadastroReserva = VagasCadastroReserva,
                    Concorrencia = Concorrencia,
                    Colocacao = Colocacao,
                    NumeroInscricao = NumeroInscricao,
                    AcertosProva = AcertosProva,
                    ErrosProva = ErrosProva,
                    BrancosProva = BrancosProva,
                    AnuladasProva = AnuladasProva,
                    TipoProvaId = TipoProvaId,
                    Link = Link,
                    DataAbertura = DataAbertura ?? DateTime.Now,
                    DataProva = DataProva ?? DateTime.Now.AddDays(30),
                    EscolaridadeId = EscolaridadeId,
                    FaseEditalId = FaseEditalId,
                    ProvaDiscursiva = ProvaDiscursiva,
                    ProvaTitulos = ProvaTitulos,
                    ProvaTaf = ProvaTaf,
                    ProvaPratica = ProvaPratica,
                    BoletoPago = BoletoPago,
                    Arquivado = IsArquivado,
                    Validade = Validade,
                    DataHomologacao = DataHomologacao,
                    EditalAssuntos = AssuntosPorDisciplina
                        .SelectMany(g => g.Assuntos)
                        .Select(a => new EditalAssunto { AssuntoId = a.Assunto.Id, Assunto = a.Assunto })
                        .ToList()
                };
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Propriedades para compatibilidade com LoadingAndEmptyStatePanel
        public bool IsCarregando
        {
            get => IsLoading;
            set => IsLoading = value;
        }

        public int FilteredCount => IsEditingAssuntos ? TotalItensEdicao : AssuntosVinculadosCount;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Implementar pesquisa com debounce
                    _searchTimer.Stop();
                    _searchTimer.Start();
                }
            }
        }

        // Coleções
        public ObservableCollection<DisciplinaAssuntoGroup> AssuntosPorDisciplina { get; } = new();
        public ObservableCollection<EditalCronograma> EventosCronograma { get; } = new();
        public ObservableCollection<EditalCronograma> CronogramaEventos { get; } = new();
        public ObservableCollection<FaseEdital> FasesEdital { get; } = new();
        public ObservableCollection<Escolaridade> Escolaridades { get; } = new();
        public ObservableCollection<TiposProva> TiposProva { get; } = new();

        [ObservableProperty]
        private string _searchTextAssuntos = string.Empty;

        partial void OnSearchTextAssuntosChanged(string value)
        {
            FiltrarAssuntosPorPesquisa(value);
            if (_assuntosDisponiveisView != null)
            {
                _assuntosDisponiveisView.Refresh();
            }
        }

        [ObservableProperty]
        private string _searchTextEventos = string.Empty;

        partial void OnSearchTextEventosChanged(string value)
        {
            FiltrarEventosPorPesquisa(value);
            if (_eventosView != null)
            {
                _eventosView.Refresh();
            }
        }

        /// <summary>
        /// Notifica mudança no percentil quando Concorrencia muda
        /// </summary>
        partial void OnConcorrenciaChanged(int? value)
        {
            OnPropertyChanged(nameof(ColocacaoPercentil));
        }

        /// <summary>
        /// Notifica mudança no percentil quando Colocacao muda
        /// </summary>
        partial void OnColocacaoChanged(int? value)
        {
            OnPropertyChanged(nameof(ColocacaoPercentil));
            // Notificar mudança no CurrentEdital para atualizar o badge em tempo real
            OnPropertyChanged(nameof(CurrentEdital));
        }

        /// <summary>
        /// Notifica mudança no CurrentEdital quando VagasImediatas muda
        /// </summary>
        partial void OnVagasImediatasChanged(int? value)
        {
            OnPropertyChanged(nameof(CurrentEdital));
        }

        /// <summary>
        /// Notifica mudança no CurrentEdital quando VagasCadastroReserva muda
        /// </summary>
        partial void OnVagasCadastroReservaChanged(int? value)
        {
            OnPropertyChanged(nameof(CurrentEdital));
        }

        /// <summary>
        /// Notifica mudança no CurrentEdital quando DataHomologacao muda
        /// </summary>
        partial void OnDataHomologacaoChanged(DateTime? value)
        {
            OnPropertyChanged(nameof(CurrentEdital));
            OnPropertyChanged(nameof(Encerrado));
        }

        [ObservableProperty]
        private bool _isEditingAssuntos = false;

        partial void OnIsEditingAssuntosChanged(bool value)
        {
            // Notificar que FilteredCount mudou quando o modo de edição muda
            OnPropertyChanged(nameof(FilteredCount));
        }

        [ObservableProperty]
        private int _paginaAtualEdicao = 1;

        [ObservableProperty]
        private int _totalPaginasEdicao = 1;

        [ObservableProperty]
        private int _totalItensEdicao = 0;

        [ObservableProperty]
        private ObservableCollection<AssuntoSelecionavel> _assuntosDisponiveisPaginados = new();

        public ObservableCollection<AssuntoSelecionavel> AssuntosDisponiveis { get; } = new();

        private ICollectionView? _assuntosDisponiveisView;
        public ICollectionView? AssuntosDisponiveisView
        {
            get => _assuntosDisponiveisView;
            private set => SetProperty(ref _assuntosDisponiveisView, value);
        }

        partial void OnPaginaAtualEdicaoChanged(int value)
        {
            FirstPageEdicaoCommand?.NotifyCanExecuteChanged();
            PreviousPageEdicaoCommand?.NotifyCanExecuteChanged();
            NextPageEdicaoCommand?.NotifyCanExecuteChanged();
            LastPageEdicaoCommand?.NotifyCanExecuteChanged();
        }

        partial void OnTotalPaginasEdicaoChanged(int value)
        {
            FirstPageEdicaoCommand?.NotifyCanExecuteChanged();
            PreviousPageEdicaoCommand?.NotifyCanExecuteChanged();
            NextPageEdicaoCommand?.NotifyCanExecuteChanged();
            LastPageEdicaoCommand?.NotifyCanExecuteChanged();
        }

        private ICollectionView? _eventosView;
        public ICollectionView? EventosView
        {
            get => _eventosView;
            private set => SetProperty(ref _eventosView, value);
        }

        public bool HasAssuntosVinculados => AssuntosPorDisciplina.Any(g => g.Assuntos.Any());
        public bool HasCronogramaEventos => EventosCronograma.Any();

        public int AssuntosVinculadosCount => AssuntosPorDisciplina.Sum(g => g.Assuntos.Count);
        public int EventosCount => EventosCronograma.Count;

        public decimal ProgressoGeral
        {
            get
            {
                if (!HasAssuntosVinculados) return 0;
                var totalAssuntos = AssuntosPorDisciplina.Sum(g => g.Assuntos.Count);
                if (totalAssuntos == 0) return 0;
                var assuntosConcluidos = AssuntosPorDisciplina.Sum(g => g.Assuntos.Count(a => a.Assunto.Concluido));
                return Math.Round((decimal)assuntosConcluidos / totalAssuntos * 100, 2);
            }
        }

        public string StatusMessage => IsSaving ? "Salvando..." : "Pronto";

        /// <summary>
        /// Implementação de IEditableViewModel - Detecta se há alterações não salvas
        /// </summary>
        public bool HasUnsavedChanges
        {
            get
            {
                // Se está em modo de edição de assuntos, há alterações
                if (IsEditingAssuntos)
                    return true;

                // Se não há edital original (novo edital), sempre há alterações
                if (_editalOriginal == null)
                    return true;

                // Usar _editalSalvo se disponível (após salvamento bem-sucedido)
                var editalParaComparar = _editalSalvo ?? _editalOriginal;

                // Comparar campos principais do edital
                if (editalParaComparar.Cargo != Cargo ||
                    editalParaComparar.Orgao != Orgao ||
                    editalParaComparar.Banca != Banca ||
                    editalParaComparar.Area != Area ||
                    editalParaComparar.Salario != Salario ||
                    editalParaComparar.ValorInscricao != ValorInscricao ||
                    editalParaComparar.VagasImediatas != VagasImediatas ||
                    editalParaComparar.VagasCadastroReserva != VagasCadastroReserva ||
                    editalParaComparar.Concorrencia != Concorrencia ||
                    editalParaComparar.Colocacao != Colocacao ||
                    editalParaComparar.NumeroInscricao != NumeroInscricao ||
                    editalParaComparar.AcertosProva != AcertosProva ||
                    editalParaComparar.ErrosProva != ErrosProva ||
                    editalParaComparar.BrancosProva != BrancosProva ||
                    editalParaComparar.AnuladasProva != AnuladasProva ||
                    editalParaComparar.TipoProvaId != TipoProvaId ||
                    editalParaComparar.Link != Link ||
                    editalParaComparar.DataAbertura != DataAbertura ||
                    editalParaComparar.DataProva != DataProva ||
                    editalParaComparar.EscolaridadeId != EscolaridadeId ||
                    editalParaComparar.FaseEditalId != FaseEditalId ||
                    editalParaComparar.ProvaDiscursiva != ProvaDiscursiva ||
                    editalParaComparar.ProvaTitulos != ProvaTitulos ||
                    editalParaComparar.ProvaTaf != ProvaTaf ||
                    editalParaComparar.ProvaPratica != ProvaPratica ||
                    editalParaComparar.BoletoPago != BoletoPago ||
                    editalParaComparar.Arquivado != IsArquivado ||
                    editalParaComparar.Validade != Validade ||
                    editalParaComparar.DataHomologacao != DataHomologacao)
                    return true;

                // Comparar assuntos vinculados
                var assuntosOriginaisIds = editalParaComparar.EditalAssuntos?.Select(ea => ea.AssuntoId).OrderBy(id => id).ToList() ?? new();
                var assuntosAtuaisIds = AssuntosPorDisciplina
                    .SelectMany(g => g.Assuntos)
                    .Select(a => a.Assunto.Id)
                    .OrderBy(id => id)
                    .ToList();

                if (!assuntosOriginaisIds.SequenceEqual(assuntosAtuaisIds))
                    return true;

                // Comparar eventos do cronograma
                var eventosOriginaisIds = editalParaComparar.EditalCronogramas?.Select(ec => ec.Id).OrderBy(id => id).ToList() ?? new();
                var eventosAtuaisIds = EventosCronograma.Select(ec => ec.Id).OrderBy(id => id).ToList();

                if (!eventosOriginaisIds.SequenceEqual(eventosAtuaisIds))
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Implementação de IEditableViewModel - Chamado ao descarregar a view
        /// Retorna true se a navegação deve ser cancelada (há alterações não salvas)
        /// </summary>
        public async Task<bool> OnViewUnloadingAsync()
        {
            if (!HasUnsavedChanges)
                return false;

            // Exibir confirmação
            var resultado = _notificationService.ShowConfirmation(
                "Alterações Não Salvas",
                "Você tem alterações não salvas. Deseja descartá-las?");

            // Retornar true se o usuário clicou "Não" (cancelar navegação)
            return resultado == ToastMessageBoxResult.No;
        }

        // Commands
        [RelayCommand]
        private void NavigateToInformacoes()
        {
            CurrentView = EditalViewType.Informacoes;
        }

        [RelayCommand]
        private void NavigateToAssuntos()
        {
            CurrentView = EditalViewType.Assuntos;
        }

        [RelayCommand]
        private void NavigateToCronograma()
        {
            CurrentView = EditalViewType.Cronograma;
        }

        [RelayCommand]
        private void ToggleEditAssuntos()
        {
            if (!IsEditingAssuntos)
            {
                // Entrar em modo de edição
                EntrarModoEdicaoAssuntos();
            }
            else
            {
                // Sair do modo de edição e aplicar mudanças
                SairModoEdicaoAssuntos();
            }
        }

        [RelayCommand(CanExecute = nameof(CanNavigateToFirstPageEdicao))]
        private void FirstPageEdicao()
        {
            if (PaginaAtualEdicao > 1)
            {
                PaginaAtualEdicao = 1;
                CarregarAssuntosDisponiveisParaSelecaoEdicao();
            }
        }

        private bool CanNavigateToFirstPageEdicao() => TotalPaginasEdicao > 1 && PaginaAtualEdicao > 1;

        [RelayCommand(CanExecute = nameof(CanNavigateToPreviousPageEdicao))]
        private void PreviousPageEdicao()
        {
            if (PaginaAtualEdicao > 1)
            {
                PaginaAtualEdicao--;
                CarregarAssuntosDisponiveisParaSelecaoEdicao();
            }
        }

        private bool CanNavigateToPreviousPageEdicao() => TotalPaginasEdicao > 1 && PaginaAtualEdicao > 1;

        [RelayCommand(CanExecute = nameof(CanNavigateToNextPageEdicao))]
        private void NextPageEdicao()
        {
            if (PaginaAtualEdicao < TotalPaginasEdicao)
            {
                PaginaAtualEdicao++;
                CarregarAssuntosDisponiveisParaSelecaoEdicao();
            }
        }

        private bool CanNavigateToNextPageEdicao() => TotalPaginasEdicao > 1 && PaginaAtualEdicao < TotalPaginasEdicao;

        [RelayCommand(CanExecute = nameof(CanNavigateToLastPageEdicao))]
        private void LastPageEdicao()
        {
            if (PaginaAtualEdicao < TotalPaginasEdicao)
            {
                PaginaAtualEdicao = TotalPaginasEdicao;
                CarregarAssuntosDisponiveisParaSelecaoEdicao();
            }
        }

        private bool CanNavigateToLastPageEdicao() => TotalPaginasEdicao > 1 && PaginaAtualEdicao < TotalPaginasEdicao;

        [RelayCommand]
        private void CancelarEdicaoAssuntos()
        {
            // Cancelar edição sem aplicar mudanças
            try
            {
                // Limpar coleção de assuntos disponíveis
                AssuntosDisponiveis.Clear();
                AssuntosDisponiveisView = null;
                AssuntosDisponiveisPaginados.Clear();

                // Limpar dicionário de seleções
                _selecoesPorAssuntoIdEdicao.Clear();

                // Limpar texto de pesquisa
                SearchText = string.Empty;

                // Resetar paginação
                PaginaAtualEdicao = 1;
                TotalPaginasEdicao = 1;
                TotalItensEdicao = 0;

                // Sair do modo de edição
                IsEditingAssuntos = false;

                // CORREÇÃO: Forçar a restauração da lista visual
                // Já que o SearchText não mudou (já era vazio), o timer não dispara.
                FiltrarAssuntosPorPesquisa(string.Empty);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Cancelar", $"Erro ao cancelar edição: {ex.Message}");
            }
        }

        private async Task SalvarAsync()
        {
            try
            {
                IsSaving = true;

                // DEBUG: Log dos valores do ViewModel
                System.Diagnostics.Debug.WriteLine($"[DEBUG] EditarEditalViewModel.SalvarAsync() iniciado");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ID do Edital: {Id}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] BrancosProva (ViewModel): {BrancosProva}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] AnuladasProva (ViewModel): {AnuladasProva}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] TipoProvaId (ViewModel): {TipoProvaId}");

                // Sincronizar estado de arquivamento com a propriedade Arquivado
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Sincronizando arquivamento: IsArquivado={IsArquivado}");

                var edital = new Edital
                {
                    Id = Id,
                    Cargo = Cargo,
                    Orgao = Orgao,
                    Banca = Banca,
                    Area = Area,
                    Salario = Salario,
                    ValorInscricao = ValorInscricao,
                    VagasImediatas = VagasImediatas,
                    VagasCadastroReserva = VagasCadastroReserva,
                    Concorrencia = Concorrencia,
                    Colocacao = Colocacao,
                    NumeroInscricao = NumeroInscricao,
                    AcertosProva = AcertosProva,
                    ErrosProva = ErrosProva,
                    BrancosProva = BrancosProva,
                    AnuladasProva = AnuladasProva,
                    TipoProvaId = TipoProvaId,
                    Link = Link,
                    DataAbertura = DataAbertura ?? DateTime.Now,
                    DataProva = DataProva ?? DateTime.Now.AddDays(30),
                    EscolaridadeId = EscolaridadeId,
                    FaseEditalId = FaseEditalId,
                    ProvaDiscursiva = ProvaDiscursiva,
                    ProvaTitulos = ProvaTitulos,
                    ProvaTaf = ProvaTaf,
                    ProvaPratica = ProvaPratica,
                    BoletoPago = BoletoPago,
                    Arquivado = IsArquivado,
                    Validade = Validade,
                    DataHomologacao = DataHomologacao
                };

                // DEBUG: Log dos valores no objeto Edital
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Objeto Edital criado:");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] BrancosProva (Edital): {edital.BrancosProva}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] AnuladasProva (Edital): {edital.AnuladasProva}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] TipoProvaId (Edital): {edital.TipoProvaId}");

                System.Diagnostics.Debug.WriteLine($"\n[SALVAR_ASYNC] ========== SALVAMENTO INICIADO ==========");
                System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC] SearchText atual: '{SearchText}'");
                System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC] _assuntosOriginaisIntactos.Count: {_assuntosOriginaisIntactos.Count}");
                System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC] _assuntosOriginais.Count: {_assuntosOriginais.Count}");
                System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC] AssuntosPorDisciplina.Count: {AssuntosPorDisciplina.Count}");

                var totalAssuntosOriginaisParaSalvar = _assuntosOriginaisIntactos.SelectMany(g => g.Assuntos).Count();
                System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC] Total de assuntos em _assuntosOriginaisIntactos: {totalAssuntosOriginaisParaSalvar}");

                // IMPORTANTE: Usar _assuntosOriginaisIntactos (dados completos e nunca filtrados)
                // em vez de _assuntosOriginais (que pode estar filtrado pela pesquisa)
                var assuntosIds = _assuntosOriginaisIntactos
                    .SelectMany(g => g.Assuntos)
                    .Select(a => a.Assunto.Id)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC] IDs dos assuntos a salvar:");
                foreach (var id in assuntosIds)
                {
                    System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC]   - ID: {id}");
                }

                // Coletar eventos do cronograma
                var cronogramaEventos = EventosCronograma.ToList();

                // DEBUG: Log antes de chamar o serviço
                System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC] Chamando SalvarEditalComRelacionamentosAsync com:");
                System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC] - Edital ID: {edital.Id}");
                System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC] - Assuntos: {assuntosIds.Count}");
                System.Diagnostics.Debug.WriteLine($"[SALVAR_ASYNC] - Eventos: {cronogramaEventos.Count}");

                // Salvar edital, assuntos e cronograma em uma única transação
                await _editalService.SalvarEditalComRelacionamentosAsync(edital, assuntosIds, cronogramaEventos);

                // DEBUG: Log após salvar
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Edital salvo com sucesso!");

                // Atualizar _editalSalvo para refletir o estado salvo
                // Isso garante que HasUnsavedChanges retorne false ao descarregar a view
                _editalSalvo = new Edital
                {
                    Id = Id,
                    Cargo = Cargo,
                    Orgao = Orgao,
                    Banca = Banca,
                    Area = Area,
                    Salario = Salario,
                    ValorInscricao = ValorInscricao,
                    VagasImediatas = VagasImediatas,
                    VagasCadastroReserva = VagasCadastroReserva,
                    Concorrencia = Concorrencia,
                    Colocacao = Colocacao,
                    NumeroInscricao = NumeroInscricao,
                    AcertosProva = AcertosProva,
                    ErrosProva = ErrosProva,
                    BrancosProva = BrancosProva,
                    AnuladasProva = AnuladasProva,
                    TipoProvaId = TipoProvaId,
                    Link = Link,
                    DataAbertura = DataAbertura ?? DateTime.Now,
                    DataProva = DataProva ?? DateTime.Now.AddDays(30),
                    EscolaridadeId = EscolaridadeId,
                    FaseEditalId = FaseEditalId,
                    ProvaDiscursiva = ProvaDiscursiva,
                    ProvaTitulos = ProvaTitulos,
                    ProvaTaf = ProvaTaf,
                    ProvaPratica = ProvaPratica,
                    BoletoPago = BoletoPago,
                    Arquivado = IsArquivado,
                    Validade = Validade,
                    DataHomologacao = DataHomologacao,
                    EditalAssuntos = _assuntosOriginaisIntactos
                        .SelectMany(g => g.Assuntos)
                        .Select(a => new EditalAssunto { AssuntoId = a.Assunto.Id, Assunto = a.Assunto })
                        .ToList(),
                    EditalCronogramas = EventosCronograma.ToList()
                };

                _notificationService.ShowSuccess("Sucesso", "Edital salvo com sucesso!");
                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ERRO em SalvarAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Stack Trace: {ex.StackTrace}");
                _notificationService.ShowError("Erro ao Salvar", $"Erro ao salvar edital: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void Cancelar()
        {
            _navigationService.GoBack();
        }

        private void AdicionarAssuntos()
        {
            _notificationService.ShowInfo("Informação", "Funcionalidade de adicionar assuntos será implementada.");
        }

        private void AdicionarEvento()
        {
            try
            {
                var novoEvento = new EditalCronograma
                {
                    Id = 0, // Novo evento não tem ID
                    EditalId = Id,
                    Evento = string.Empty,
                    DataEvento = DateTime.Now.AddDays(7),
                    Concluido = false,
                    Ignorado = false,
                    IsEditing = true
                };

                EventosCronograma.Add(novoEvento);
                OnPropertyChanged(nameof(EventosCount));
                OnPropertyChanged(nameof(HasCronogramaEventos));

                // Atualizar o EventosView para incluir o novo evento
                if (EventosView != null)
                {
                    EventosView.Refresh();
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Adicionar", $"Erro ao adicionar evento: {ex.Message}");
            }
        }

        private void RemoverEvento(EditalCronograma evento)
        {
            if (evento != null && _notificationService.ShowConfirmation("Confirmação", "Deseja excluir este evento?") == ToastMessageBoxResult.Yes)
            {
                EventosCronograma.Remove(evento);
                OnPropertyChanged(nameof(EventosCount));
                OnPropertyChanged(nameof(HasCronogramaEventos));
            }
        }

        private void IniciarEdicaoEvento(EditalCronograma evento)
        {
            if (evento != null)
            {
                // Criar backup do evento antes de editar
                var backup = new EditalCronograma
                {
                    Id = evento.Id,
                    EditalId = evento.EditalId,
                    Evento = evento.Evento,
                    DataEventoTicks = evento.DataEventoTicks,
                    Concluido = evento.Concluido,
                    Ignorado = evento.Ignorado,
                    IsEditing = false
                };
                _cronogramaBackup[evento] = backup;
                evento.IsEditing = true;
            }
        }

        private void SalvarEvento(EditalCronograma evento)
        {
            if (evento != null)
            {
                // Validar dados do evento
                if (string.IsNullOrWhiteSpace(evento.Evento))
                {
                    _notificationService.ShowWarning("Validação", "O nome do evento é obrigatório.");
                    return;
                }

                // Remover backup após salvar com sucesso
                if (_cronogramaBackup.ContainsKey(evento))
                {
                    _cronogramaBackup.Remove(evento);
                }

                evento.IsEditing = false;
                evento.AtualizarDataModificacao();
                OnPropertyChanged(nameof(EventosCount));
                OnPropertyChanged(nameof(HasCronogramaEventos));

                // Atualizar o EventosView para refletir as mudanças na interface
                if (EventosView != null)
                {
                    EventosView.Refresh();
                }
            }
        }

        private void CancelarEdicaoEvento(EditalCronograma evento)
        {
            if (evento != null)
            {
                // Se for um evento novo (Id == 0), remover da lista
                if (evento.Id == 0)
                {
                    EventosCronograma.Remove(evento);
                    OnPropertyChanged(nameof(EventosCount));
                    OnPropertyChanged(nameof(HasCronogramaEventos));
                }
                else
                {
                    // Se houver backup, restaurar os valores originais
                    if (_cronogramaBackup.TryGetValue(evento, out var backup))
                    {
                        evento.Evento = backup.Evento;
                        evento.DataEventoTicks = backup.DataEventoTicks;
                        evento.Concluido = backup.Concluido;
                        evento.Ignorado = backup.Ignorado;
                        _cronogramaBackup.Remove(evento);
                    }
                }

                evento.IsEditing = false;
            }
        }

        private void AbrirLinkEdital()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Link))
                {
                    _notificationService.ShowWarning("Aviso", "Nenhum link foi configurado para este edital.");
                    return;
                }

                // Validar se é uma URL válida
                if (!Link.StartsWith("http://") && !Link.StartsWith("https://"))
                {
                    _notificationService.ShowWarning("Aviso", "O link não é uma URL válida (deve começar com http:// ou https://).");
                    return;
                }

                // Abrir o link no navegador padrão
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Link,
                    UseShellExecute = true
                });

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Link do edital aberto: {Link}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Erro ao abrir link: {ex.Message}");
                _notificationService.ShowError("Erro ao Abrir", $"Erro ao abrir o link do edital: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ToggleExpandDisciplina(DisciplinaAssuntoGroup group)
        {
            if (group != null)
            {
                group.IsExpanded = !group.IsExpanded;
            }
        }

        [RelayCommand]
        private void VerDetalhesAssunto(AssuntoVinculado assuntoVinculado)
        {
            if (assuntoVinculado?.Assunto != null)
            {
                NotificationService.Instance.ShowInfo(
                    "Detalhes do Assunto",
                    $"Detalhes do assunto: {assuntoVinculado.Assunto.Nome}\n\n" +
                    $"Disciplina: {assuntoVinculado.Disciplina.Nome}\n" +
                    $"Status: {(assuntoVinculado.Assunto.Concluido ? "Concluído" : "Pendente")}\n" +
                    $"Progresso: {assuntoVinculado.Assunto.Progresso ?? 0}%");
            }
        }

        [RelayCommand]
        private void RemoverAssunto(AssuntoVinculado assunto)
        {
            if (assunto != null)
            {
                var group = AssuntosPorDisciplina.FirstOrDefault(g => g.Assuntos.Contains(assunto));
                if (group != null)
                {
                    group.Assuntos.Remove(assunto);
                    if (!group.Assuntos.Any())
                    {
                        AssuntosPorDisciplina.Remove(group);
                    }
                    OnPropertyChanged(nameof(HasAssuntosVinculados));
                    OnPropertyChanged(nameof(ProgressoGeral));
                }
            }
        }

        [RelayCommand]
        private void AdicionarCronograma()
        {
            var novoEvento = new EditalCronograma
            {
                EditalId = Id,
                Evento = "Novo Evento",
                DataEvento = DateTime.Now.AddDays(7),
                Concluido = false,
                Ignorado = false
            };

            CronogramaEventos.Add(novoEvento);
            OnPropertyChanged(nameof(HasCronogramaEventos));
        }

        [RelayCommand]
        private void ToggleEditarCronograma(EditalCronograma evento)
        {
            // Implementar toggle de edição inline
            _notificationService.ShowInfo("Informação", "Edição inline do cronograma será implementada.");
        }

        [RelayCommand]
        private void ExcluirCronograma(EditalCronograma evento)
        {
            if (evento != null && _notificationService.ShowConfirmation("Confirmação", "Deseja excluir este evento?") == ToastMessageBoxResult.Yes)
            {
                CronogramaEventos.Remove(evento);
                OnPropertyChanged(nameof(HasCronogramaEventos));
            }
        }

        private async void CarregarDadosIniciais()
        {
            try
            {
                IsLoading = true;

                // Carregar fases, escolaridades e tipos de prova
                await CarregarFasesEdital();
                await CarregarEscolaridades();
                await CarregarTiposProva();

                if (_editalOriginal != null)
                {
                    // Carregar assuntos vinculados
                    await CarregarAssuntosVinculados();

                    // Carregar cronograma
                    await CarregarCronograma();
                }
                else
                {
                    // Para novos editais, inicializar o EventosView mesmo sem eventos
                    if (EventosView == null)
                    {
                        EventosView = CollectionViewSource.GetDefaultView(EventosCronograma);
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar dados: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CarregarAssuntosVinculados()
        {
            try
            {
                if (_editalOriginal == null) return;

                // Carregar EditalAssuntos com include para Assunto e Disciplina
                var editalAssuntos = await _context.EditalAssuntos
                    .Where(ea => ea.EditalId == _editalOriginal.Id)
                    .Include(ea => ea.Assunto)
                    .ThenInclude(a => a.Disciplina)
                    .ToListAsync();

                var assuntosVinculados = editalAssuntos.Select(ea => new AssuntoVinculado
                {
                    Disciplina = ea.Assunto.Disciplina,
                    Assunto = ea.Assunto
                }).ToList();

                var grouped = assuntosVinculados
                    .GroupBy(a => a.Disciplina.Nome)
                    .Select(g => new DisciplinaAssuntoGroup
                    {
                        Disciplina = g.First().Disciplina,
                        Assuntos = new ObservableCollection<AssuntoVinculado>(g)
                    }).ToList();

                // Armazenar os assuntos originais INTACTOS (nunca filtrados) para salvamento
                _assuntosOriginaisIntactos = grouped.Select(g => new DisciplinaAssuntoGroup
                {
                    Disciplina = g.Disciplina,
                    Assuntos = new ObservableCollection<AssuntoVinculado>(g.Assuntos)
                }).ToList();

                // Armazenar também em _assuntosOriginais para exibição (pode ser filtrado)
                _assuntosOriginais = grouped;

                foreach (var group in grouped)
                {
                    AssuntosPorDisciplina.Add(group);
                }

                OnPropertyChanged(nameof(HasAssuntosVinculados));
                OnPropertyChanged(nameof(ProgressoGeral));
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar assuntos vinculados: {ex.Message}");
            }
        }

        private async Task CarregarCronograma()
        {
            try
            {
                if (_editalOriginal == null) return;

                // Carregar eventos do cronograma do banco de dados
                // Usar DataEventoTicks pois DataEvento é [NotMapped]
                var eventos = await _context.EditalCronograma
                    .Where(ec => ec.EditalId == _editalOriginal.Id)
                    .OrderBy(ec => ec.DataEventoTicks)
                    .ToListAsync();

                foreach (var evento in eventos)
                {
                    evento.IsEditing = false;
                    EventosCronograma.Add(evento);
                }

                // Inicializar o EventosView após carregar os eventos
                if (EventosView == null)
                {
                    EventosView = CollectionViewSource.GetDefaultView(EventosCronograma);
                }

                OnPropertyChanged(nameof(EventosCount));
                OnPropertyChanged(nameof(HasCronogramaEventos));
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar cronograma: {ex.Message}");
            }
        }

        private async Task CarregarFasesEdital()
        {
            try
            {
                var fases = await _context.FasesEdital.OrderBy(f => f.Fase).ToListAsync();

                FasesEdital.Clear();

                //// Adicionar opção vazia
                //FasesEdital.Add(new FaseEdital { Id = 0, Fase = "Selecione uma fase..." });

                foreach (var fase in fases)
                {
                    FasesEdital.Add(fase);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar fases: {ex.Message}");
            }
        }

        private async Task CarregarEscolaridades()
        {
            try
            {
                var escolaridades = await _context.Escolaridades.OrderBy(e => e.Nome).ToListAsync();

                Escolaridades.Clear();

                //// Adicionar opção vazia
                //Escolaridades.Add(new Escolaridade { Id = 0, Nome = "Selecione uma escolaridade..." });

                foreach (var escolaridade in escolaridades)
                {
                    Escolaridades.Add(escolaridade);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar escolaridades: {ex.Message}");
            }
        }

        private async Task CarregarTiposProva()
        {
            try
            {
                var tiposProva = await _context.TiposProva.OrderBy(t => t.Nome).ToListAsync();

                TiposProva.Clear();

                foreach (var tipo in tiposProva)
                {
                    TiposProva.Add(tipo);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar tipos de prova: {ex.Message}");
            }
        }

        private void FiltrarAssuntosPorPesquisa(string searchText)
        {
            System.Diagnostics.Debug.WriteLine($"\n[FILTRAR_PESQUISA] ========== FILTRAGEM INICIADA ==========");
            System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] SearchText: '{searchText}'");
            System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] _assuntosOriginais.Count ANTES: {_assuntosOriginais.Count}");
            var totalAssuntosOriginais = _assuntosOriginais.SelectMany(g => g.Assuntos).Count();
            System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] Total de assuntos em _assuntosOriginais: {totalAssuntosOriginais}");

            AssuntosPorDisciplina.Clear();
            System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] AssuntosPorDisciplina.Clear() executado");

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Se não há texto de pesquisa, mostrar todos os assuntos originais
                System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] SearchText vazio - Copiando todos os assuntos originais");
                foreach (var group in _assuntosOriginais)
                {
                    var novoGrupo = new DisciplinaAssuntoGroup
                    {
                        Disciplina = group.Disciplina,
                        IsExpanded = true
                    };

                    foreach (var assunto in group.Assuntos)
                    {
                        novoGrupo.Assuntos.Add(assunto);
                    }

                    AssuntosPorDisciplina.Add(novoGrupo);
                    System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] Adicionado grupo: {group.Disciplina.Nome} com {group.Assuntos.Count} assuntos");
                }
            }
            else
            {
                // Filtrar assuntos por texto de pesquisa (case-insensitive)
                System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] Filtrando por: '{searchText}'");
                var searchTextLower = searchText.ToLower();
                var assuntosFiltrados = new List<DisciplinaAssuntoGroup>();

                foreach (var group in _assuntosOriginais)
                {
                    var assuntosGrupo = group.Assuntos
                        .Where(a => a.Assunto.Nome.ToLower().Contains(searchTextLower))
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] Disciplina '{group.Disciplina.Nome}': {assuntosGrupo.Count} de {group.Assuntos.Count} assuntos correspondem");

                    if (assuntosGrupo.Any())
                    {
                        var novoGrupo = new DisciplinaAssuntoGroup
                        {
                            Disciplina = group.Disciplina,
                            IsExpanded = true // Manter expandido durante a pesquisa
                        };

                        foreach (var assunto in assuntosGrupo)
                        {
                            novoGrupo.Assuntos.Add(assunto);
                            System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA]   - Assunto adicionado: {assunto.Assunto.Nome}");
                        }

                        assuntosFiltrados.Add(novoGrupo);
                    }
                }

                // Adicionar grupos filtrados à coleção
                System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] Total de grupos filtrados: {assuntosFiltrados.Count}");
                foreach (var group in assuntosFiltrados)
                {
                    AssuntosPorDisciplina.Add(group);
                }
            }

            var totalAssuntosExibidos = AssuntosPorDisciplina.SelectMany(g => g.Assuntos).Count();
            System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] AssuntosPorDisciplina.Count DEPOIS: {AssuntosPorDisciplina.Count}");
            System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] Total de assuntos exibidos: {totalAssuntosExibidos}");

            OnPropertyChanged(nameof(HasAssuntosVinculados));
            OnPropertyChanged(nameof(AssuntosVinculadosCount));
            OnPropertyChanged(nameof(FilteredCount));
            System.Diagnostics.Debug.WriteLine($"[FILTRAR_PESQUISA] ========== FILTRAGEM FINALIZADA ==========\n");
        }

        private void FiltrarEventosPorPesquisa(string searchText)
        {
            if (EventosView == null)
            {
                EventosView = CollectionViewSource.GetDefaultView(EventosCronograma);
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                EventosView.Filter = null;
            }
            else
            {
                var searchLower = searchText.ToLower();
                EventosView.Filter = obj =>
                {
                    if (obj is EditalCronograma evento)
                    {
                        return evento.Evento.ToLower().Contains(searchLower);
                    }
                    return false;
                };
            }
        }

        /// <summary>
        /// Entra no modo de edição de assuntos em massa.
        /// Carrega todos os assuntos disponíveis e marca os já vinculados como selecionados.
        /// </summary>
        private void EntrarModoEdicaoAssuntos()
        {
            try
            {
                IsLoading = true;

                // Limpar coleções anteriores
                AssuntosDisponiveis.Clear();
                AssuntosDisponiveisPaginados.Clear();
                _selecoesPorAssuntoIdEdicao.Clear();

                // Obter todos os assuntos do banco de dados
                var todosAssuntos = _context.Assuntos
                    .Include(a => a.Disciplina)
                    .Where(a => !a.Arquivado)
                    .OrderBy(a => a.Disciplina.Nome)
                    .ThenBy(a => a.Nome)
                    .ToList();

                // Obter IDs dos assuntos já vinculados ao edital
                var assuntosVinculadosIds = _assuntosOriginaisIntactos
            .SelectMany(g => g.Assuntos)
            .Select(a => a.Assunto.Id)
            .ToHashSet();

                // Criar callback para sincronizar seleções
                Action<int, bool> onSelectionChanged = (assuntoId, isSelected) =>
                {
                    _selecoesPorAssuntoIdEdicao[assuntoId] = isSelected;
                };

                // Criar wrappers para cada assunto
                foreach (var assunto in todosAssuntos)
                {
                    var isSelected = assuntosVinculadosIds.Contains(assunto.Id);
                    AssuntosDisponiveis.Add(new AssuntoSelecionavel(assunto, isSelected, onSelectionChanged));
                    _selecoesPorAssuntoIdEdicao[assunto.Id] = isSelected;
                }

                // Calcular paginação
                TotalItensEdicao = AssuntosDisponiveis.Count;
                TotalPaginasEdicao = (int)Math.Ceiling((double)TotalItensEdicao / _itensPorPaginaEdicao);
                PaginaAtualEdicao = 1;

                // Limpar texto de pesquisa
                SearchText = string.Empty;

                // Carregar primeira página
                CarregarAssuntosDisponiveisParaSelecaoEdicao();

                // Entrar em modo de edição
                IsEditingAssuntos = true;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar assuntos para edição: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Carrega assuntos disponíveis para seleção com paginação e filtragem.
        /// Persiste seleções entre mudanças de página.
        /// </summary>
        private void CarregarAssuntosDisponiveisParaSelecaoEdicao()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"\n[CARREGAR_EDICAO] ========== CARREGAMENTO MODO EDIÇÃO INICIADO ==========");
                System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] SearchText: '{SearchText}'");
                System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] AssuntosDisponiveis.Count: {AssuntosDisponiveis.Count}");
                System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] _selecoesPorAssuntoIdEdicao.Count: {_selecoesPorAssuntoIdEdicao.Count}");

                AssuntosDisponiveisPaginados.Clear();

                // Filtrar assuntos baseado no texto de pesquisa (case-insensitive e sem acentos)
                var assuntosFiltrados = AssuntosDisponiveis.Where(a =>
                {
                    if (string.IsNullOrWhiteSpace(SearchText))
                        return true;

                    return Utils.StringNormalizationHelper.ContainsIgnoreCaseAndAccents(a.Assunto.Nome, SearchText) ||
                           (a.Assunto.Disciplina != null && Utils.StringNormalizationHelper.ContainsIgnoreCaseAndAccents(a.Assunto.Disciplina.Nome, SearchText));
                }).ToList();

                System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] Assuntos após filtro: {assuntosFiltrados.Count}");

                // Atualizar totais para paginação
                TotalItensEdicao = assuntosFiltrados.Count;
                TotalPaginasEdicao = (int)Math.Ceiling((double)TotalItensEdicao / _itensPorPaginaEdicao);

                System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] TotalItensEdicao: {TotalItensEdicao}, TotalPaginasEdicao: {TotalPaginasEdicao}");

                if (PaginaAtualEdicao > TotalPaginasEdicao && TotalPaginasEdicao > 0)
                    PaginaAtualEdicao = TotalPaginasEdicao;

                // Aplicar paginação aos resultados filtrados
                var startIndex = (PaginaAtualEdicao - 1) * _itensPorPaginaEdicao;
                var endIndex = Math.Min(startIndex + _itensPorPaginaEdicao, assuntosFiltrados.Count);

                System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] Paginação: startIndex={startIndex}, endIndex={endIndex}");

                for (int i = startIndex; i < endIndex; i++)
                {
                    var wrapper = assuntosFiltrados[i];

                    // Restaurar estado de seleção do dicionário
                    if (_selecoesPorAssuntoIdEdicao.ContainsKey(wrapper.Assunto.Id))
                    {
                        wrapper.IsSelected = _selecoesPorAssuntoIdEdicao[wrapper.Assunto.Id];
                        System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] Assunto {wrapper.Assunto.Nome}: IsSelected={wrapper.IsSelected} (do dicionário)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] Assunto {wrapper.Assunto.Nome}: IsSelected={wrapper.IsSelected} (padrão)");
                    }

                    AssuntosDisponiveisPaginados.Add(wrapper);
                }

                System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] AssuntosDisponiveisPaginados.Count: {AssuntosDisponiveisPaginados.Count}");
                System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] ========== CARREGAMENTO MODO EDIÇÃO FINALIZADO ==========\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] ERRO: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CARREGAR_EDICAO] Stack: {ex.StackTrace}");
                _notificationService.ShowError("Erro ao Carregar", $"Erro ao carregar assuntos para seleção: {ex.Message}");
            }
        }

        /// <summary>
        /// Sai do modo de edição de assuntos e aplica as mudanças localmente.
        /// Não persiste no banco de dados - apenas atualiza o modelo local.
        /// </summary>
        private void SairModoEdicaoAssuntos()
        {
            try
            {
                // 1. Identificar mudanças (IDs)
                var assuntosSelecionadosIds = _selecoesPorAssuntoIdEdicao
                    .Where(kvp => kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToHashSet();

                // IMPORTANTE: Use _assuntosOriginaisIntactos aqui, conforme a correção anterior
                var assuntosVinculadosIds = _assuntosOriginaisIntactos
                    .SelectMany(g => g.Assuntos)
                    .Select(a => a.Assunto.Id)
                    .ToHashSet();

                var novasSelecoes = assuntosSelecionadosIds.Except(assuntosVinculadosIds).ToList();
                var remocoes = assuntosVinculadosIds.Except(assuntosSelecionadosIds).ToList();

                // ---------------------------------------------------------
                // O BLOCO DE LIMPEZA ESTAVA AQUI - MOVA ELE PARA O FINAL 👇
                // ---------------------------------------------------------

                // 2. Atualizar _assuntosOriginaisIntactos (ADIÇÕES)
                foreach (var assuntoId in novasSelecoes)
                {
                    // Agora isso vai funcionar porque AssuntosDisponiveis ainda está cheia
                    var assunto = AssuntosDisponiveis
                        .FirstOrDefault(a => a.Assunto.Id == assuntoId)?
                        .Assunto;

                    if (assunto != null)
                    {
                        var grupo = _assuntosOriginaisIntactos
                            .FirstOrDefault(g => g.Disciplina.Id == assunto.DisciplinaId);

                        if (grupo == null)
                        {
                            grupo = new DisciplinaAssuntoGroup
                            {
                                Disciplina = assunto.Disciplina,
                                IsExpanded = true
                            };
                            _assuntosOriginaisIntactos.Add(grupo);
                        }

                        if (!grupo.Assuntos.Any(a => a.Assunto.Id == assunto.Id))
                        {
                            grupo.Assuntos.Add(new AssuntoVinculado
                            {
                                Disciplina = assunto.Disciplina,
                                Assunto = assunto
                            });
                        }
                    }
                }

                // 3. Atualizar _assuntosOriginaisIntactos (REMOÇÕES)
                foreach (var assuntoId in remocoes)
                {
                    foreach (var grupo in _assuntosOriginaisIntactos.ToList())
                    {
                        var assuntoParaRemover = grupo.Assuntos
                            .FirstOrDefault(a => a.Assunto.Id == assuntoId);

                        if (assuntoParaRemover != null)
                        {
                            grupo.Assuntos.Remove(assuntoParaRemover);

                            if (!grupo.Assuntos.Any())
                            {
                                _assuntosOriginaisIntactos.Remove(grupo);
                            }
                        }
                    }
                }

                // 4. Reconstruir a lista visual (AssuntosPorDisciplina) baseada na Intacta atualizada
                AssuntosPorDisciplina.Clear();
                foreach (var grupo in _assuntosOriginaisIntactos)
                {
                    AssuntosPorDisciplina.Add(new DisciplinaAssuntoGroup
                    {
                        Disciplina = grupo.Disciplina,
                        Assuntos = new ObservableCollection<AssuntoVinculado>(grupo.Assuntos),
                        IsExpanded = true
                    });
                }

                // Atualizar também a lista _assuntosOriginais (usada para filtros futuros)
                _assuntosOriginais = _assuntosOriginaisIntactos
                     .Select(g => new DisciplinaAssuntoGroup
                     {
                         Disciplina = g.Disciplina,
                         Assuntos = new ObservableCollection<AssuntoVinculado>(g.Assuntos),
                         IsExpanded = true
                     }).ToList();

                // 5. AGORA SIM, LIMPEZA DAS VARIÁVEIS DE EDIÇÃO (MOVIDO DO INÍCIO) ✅
                AssuntosDisponiveis.Clear();
                AssuntosDisponiveisPaginados.Clear();
                AssuntosDisponiveisView = null;
                _selecoesPorAssuntoIdEdicao.Clear();
                PaginaAtualEdicao = 1;
                TotalPaginasEdicao = 1;
                TotalItensEdicao = 0;
                SearchText = string.Empty; // Limpar busca ao sair

                // Notificar UI
                OnPropertyChanged(nameof(HasAssuntosVinculados));
                OnPropertyChanged(nameof(AssuntosVinculadosCount));
                OnPropertyChanged(nameof(FilteredCount));

                IsEditingAssuntos = false;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Erro ao Aplicar", $"Erro ao aplicar mudanças: {ex.Message}");
            }
        }

        /// <summary>
        /// Manipulador para quando uma revisão é adicionada
        /// Invalida o cache de edital
        /// </summary>
        private void OnRevisaoAdicionada(object? sender, RevisaoEventArgs e)
        {
            _cacheEdital = null;
        }

        /// <summary>
        /// Manipulador para quando uma revisão é atualizada
        /// Invalida o cache de edital
        /// </summary>
        private void OnRevisaoAtualizada(object? sender, RevisaoEventArgs e)
        {
            _cacheEdital = null;
        }

        /// <summary>
        /// Manipulador para quando uma revisão é removida
        /// Invalida o cache de edital
        /// </summary>
        private void OnRevisaoRemovida(object? sender, RevisaoEventArgs e)
        {
            _cacheEdital = null;
        }

        /// <summary>
        /// Manipulador do timer de debounce para pesquisa
        /// Executa a pesquisa após 300ms de inatividade
        /// </summary>
        private void SearchTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"\n[SEARCH_TIMER] ========== PESQUISA INICIADA ==========");
            System.Diagnostics.Debug.WriteLine($"[SEARCH_TIMER] SearchText: '{SearchText}'");
            System.Diagnostics.Debug.WriteLine($"[SEARCH_TIMER] IsEditingAssuntos: {IsEditingAssuntos}");
            System.Diagnostics.Debug.WriteLine($"[SEARCH_TIMER] _assuntosOriginais.Count: {_assuntosOriginais.Count}");
            System.Diagnostics.Debug.WriteLine($"[SEARCH_TIMER] AssuntosPorDisciplina.Count: {AssuntosPorDisciplina.Count}");

            // Executar na thread da UI para evitar erro de CollectionView
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // CORREÇÃO: Verificar explicitamente se estamos na aba Cronograma
                if (IsCronogramaView)
                {
                    System.Diagnostics.Debug.WriteLine($"[SEARCH_TIMER] Modo CRONOGRAMA - Filtrando eventos");
                    FiltrarEventosPorPesquisa(SearchText);

                    if (EventosView != null)
                    {
                        EventosView.Refresh();
                    }
                }
                else if (IsEditingAssuntos)
                {
                    // Em modo edição de assuntos (mantido como estava)
                    System.Diagnostics.Debug.WriteLine($"[SEARCH_TIMER] Modo EDIÇÃO - Carregando com paginação");
                    PaginaAtualEdicao = 1;
                    CarregarAssuntosDisponiveisParaSelecaoEdicao();
                }
                else
                {
                    // Em modo visualização de assuntos (mantido como estava)
                    System.Diagnostics.Debug.WriteLine($"[SEARCH_TIMER] Modo VISUALIZAÇÃO - Filtrando grupos");
                    FiltrarAssuntosPorPesquisa(SearchText);

                    if (AssuntosDisponiveisView != null)
                    {
                        AssuntosDisponiveisView.Refresh();
                    }
                }

            });
            System.Diagnostics.Debug.WriteLine($"[SEARCH_TIMER] ========== PESQUISA FINALIZADA ==========\n");
        }
    }

    // Classes auxiliares
    public class DisciplinaAssuntoGroup : ObservableObject
    {
        private bool _isExpanded = true;

        public Disciplina Disciplina { get; set; } = null!;
        public ObservableCollection<AssuntoVinculado> Assuntos { get; set; } = new();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

    }

    public class AssuntoVinculado
    {
        public Disciplina Disciplina { get; set; } = null!;
        public Assunto Assunto { get; set; } = null!;

        /// <summary>
        /// Calcula os acertos do assunto dentro do período do edital (DataAbertura até DataProva).
        /// </summary>
        public int GetAcertosPorPeriodo(DateTime dataAbertura, DateTime dataProva)
        {
            if (Assunto?.Estudos == null) return 0;

            return Assunto.Estudos
                .Where(e => e.DataTicks >= dataAbertura.Ticks && e.DataTicks <= dataProva.Ticks)
                .Sum(e => e.Acertos);
        }

        /// <summary>
        /// Calcula os erros do assunto dentro do período do edital (DataAbertura até DataProva).
        /// </summary>
        public int GetErrosPorPeriodo(DateTime dataAbertura, DateTime dataProva)
        {
            if (Assunto?.Estudos == null) return 0;

            return Assunto.Estudos
                .Where(e => e.DataTicks >= dataAbertura.Ticks && e.DataTicks <= dataProva.Ticks)
                .Sum(e => e.Erros);
        }

        /// <summary>
        /// Calcula o total de questões (acertos + erros) do assunto dentro do período do edital.
        /// </summary>
        public int GetTotalQuestoesPorPeriodo(DateTime dataAbertura, DateTime dataProva)
        {
            var acertos = GetAcertosPorPeriodo(dataAbertura, dataProva);
            var erros = GetErrosPorPeriodo(dataAbertura, dataProva);
            return acertos + erros;
        }

        /// <summary>
        /// Calcula o rendimento (percentual de acertos) do assunto dentro do período do edital.
        /// </summary>
        public double GetRendimentoPorPeriodo(DateTime dataAbertura, DateTime dataProva)
        {
            var totalQuestoes = GetTotalQuestoesPorPeriodo(dataAbertura, dataProva);
            if (totalQuestoes == 0) return 0;

            var acertos = GetAcertosPorPeriodo(dataAbertura, dataProva);
            return (double)acertos / totalQuestoes * 100;
        }

        /// <summary>
        /// Calcula as horas estudadas do assunto dentro do período do edital (DataAbertura até DataProva).
        /// </summary>
        public double GetHorasEstudadasPorPeriodo(DateTime dataAbertura, DateTime dataProva)
        {
            if (Assunto?.Estudos == null) return 0;

            return Assunto.Estudos
                .Where(e => e.DataTicks >= dataAbertura.Ticks && e.DataTicks <= dataProva.Ticks)
                .Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours);
        }
    }
}
