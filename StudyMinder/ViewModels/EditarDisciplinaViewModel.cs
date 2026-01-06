using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Navigation;
using static StudyMinder.Services.NotificationService;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System;

namespace StudyMinder.ViewModels
{
    public partial class EditarDisciplinaViewModel : ObservableValidator, IEditableViewModel
    {
        private readonly DisciplinaService _disciplinaService;
        private readonly AssuntoService _assuntoService;
        private readonly DisciplinaAssuntoTransactionService _transactionService;
        private readonly NavigationService _navigationService;
        private Disciplina _disciplina;
        private readonly Dictionary<string, List<string>> _validationErrors = new();
        private CancellationTokenSource? _debounceTokenSource;
        private readonly System.Timers.Timer _searchTimer;
        
        // Rastrear assuntos modificados para otimizar salvamento
        private readonly HashSet<int> _assuntosModificados = new();

        // Comandos
        public IAsyncRelayCommand SalvarAsyncCommand { get; }
        public IRelayCommand CancelarCommand { get; }
        public IRelayCommand AdicionarAssuntoCommand { get; }
        public IRelayCommand AdicionarAssuntosEmLoteCommand { get; }
        public IRelayCommand<Assunto> RemoverAssuntoCommand { get; }
        public IRelayCommand<Assunto> AbrirAssuntoCommand { get; }
        public IRelayCommand<Assunto> MoverAssuntoCommand { get; }
        public IRelayCommand<Assunto> ToggleEditarAssuntoCommand { get; }
        public IRelayCommand<Assunto> IniciarEdicaoAssuntoCommand { get; }
        public IRelayCommand<Assunto> CancelarEdicaoAssuntoCommand { get; }
        public IRelayCommand<string> SelecionarCorCommand { get; }
        
        // Comandos de paginação
        public IRelayCommand FirstPageCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand LastPageCommand { get; }

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private bool _isEditing;
        
        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private bool _isCarregando;

        // Coleção de assuntos em memória
        [ObservableProperty]
        private ObservableCollection<Assunto> _assuntosSelecionados = new();
        
        // Lista completa de assuntos (para filtragem)
        private List<Assunto> _todosAssuntos = new();
        
        // Texto de pesquisa
        [ObservableProperty]
        private string _searchText = string.Empty;

        // Propriedades de paginação
        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _filteredCount = 0;

        [ObservableProperty]
        private int _itemsPerPage = 10;

        // Lista de assuntos filtrados (para paginação)
        private List<Assunto> _assuntosFiltrados = new();

        // Assuntos a serem removidos (para exclusão no banco)
        private readonly List<Assunto> _assuntosParaRemover = new();
        
        // Assuntos a serem movidos (para outra disciplina)
        private readonly List<(Assunto assunto, int disciplinaDestinoId)> _assuntosParaMover = new();

        /// <summary>
        /// Rastreia operações de remoção de assuntos com contexto completo (cascata vs movimentação).
        /// Necessário para aplicar a movimentação de estudos corretamente durante o salvamento.
        /// </summary>
        private readonly List<Models.RemocaoAssuntoResultado> _remocoesComContexto = new();

        // Propriedade para controlar qual assunto está em modo de edição
        private Assunto? _assuntoEmEdicao;

        // Construtor sem parâmetros para o designer XAML
        public EditarDisciplinaViewModel() 
        {
            _disciplinaService = null!;
            _assuntoService = null!;
            _navigationService = null!;
            _disciplina = new Disciplina();
            Title = "Nova Disciplina";
            
            // Timer para debounce da pesquisa
            _searchTimer = new System.Timers.Timer(300);
            _searchTimer.Elapsed += SearchTimer_Elapsed;
            _searchTimer.AutoReset = false;
            
            // Inicializar comandos mesmo para o designer
            SalvarAsyncCommand = new AsyncRelayCommand(SalvarAsync);
            CancelarCommand = new RelayCommand(Cancelar);
            AdicionarAssuntoCommand = new RelayCommand(AdicionarAssunto);
            AdicionarAssuntosEmLoteCommand = new RelayCommand(AdicionarAssuntosEmLote);
            RemoverAssuntoCommand = new RelayCommand<Assunto>(RemoverAssunto);
            AbrirAssuntoCommand = new RelayCommand<Assunto>(AbrirAssunto);
            MoverAssuntoCommand = new RelayCommand<Assunto>(MoverAssunto);
            ToggleEditarAssuntoCommand = new RelayCommand<Assunto>(ToggleEditarAssunto);
            IniciarEdicaoAssuntoCommand = new RelayCommand<Assunto>(IniciarEdicaoAssunto);
            CancelarEdicaoAssuntoCommand = new RelayCommand<Assunto>(CancelarEdicaoAssunto);
            SelecionarCorCommand = new RelayCommand<string>(SelecionarCor);
            
            // Comandos de paginação com predicados
            FirstPageCommand = new RelayCommand(GoToFirstPage, CanGoToFirstPage);
            PreviousPageCommand = new RelayCommand(GoToPreviousPage, CanGoToPreviousPage);
            NextPageCommand = new RelayCommand(GoToNextPage, CanGoToNextPage);
            LastPageCommand = new RelayCommand(GoToLastPage, CanGoToLastPage);
        }

        public EditarDisciplinaViewModel(DisciplinaService disciplinaService, AssuntoService assuntoService, DisciplinaAssuntoTransactionService transactionService, NavigationService navigationService, Disciplina? disciplina = null)
        {
            _disciplinaService = disciplinaService ?? throw new ArgumentNullException(nameof(disciplinaService));
            _assuntoService = assuntoService ?? throw new ArgumentNullException(nameof(assuntoService));
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            // Timer para debounce da pesquisa
            _searchTimer = new System.Timers.Timer(300); // 300ms de delay
            _searchTimer.Elapsed += SearchTimer_Elapsed;
            _searchTimer.AutoReset = false;

            // Inicializar comandos explicitamente
            SalvarAsyncCommand = new AsyncRelayCommand(SalvarAsync);
            CancelarCommand = new RelayCommand(Cancelar);
            AdicionarAssuntoCommand = new RelayCommand(AdicionarAssunto);
            AdicionarAssuntosEmLoteCommand = new RelayCommand(AdicionarAssuntosEmLote);
            RemoverAssuntoCommand = new RelayCommand<Assunto>(RemoverAssunto);
            AbrirAssuntoCommand = new RelayCommand<Assunto>(AbrirAssunto);
            MoverAssuntoCommand = new RelayCommand<Assunto>(MoverAssunto);
            ToggleEditarAssuntoCommand = new RelayCommand<Assunto>(ToggleEditarAssunto);
            IniciarEdicaoAssuntoCommand = new RelayCommand<Assunto>(IniciarEdicaoAssunto);
            CancelarEdicaoAssuntoCommand = new RelayCommand<Assunto>(CancelarEdicaoAssunto);
            SelecionarCorCommand = new RelayCommand<string>(SelecionarCor);
            
            // Comandos de paginação com predicados
            FirstPageCommand = new RelayCommand(GoToFirstPage, CanGoToFirstPage);
            PreviousPageCommand = new RelayCommand(GoToPreviousPage, CanGoToPreviousPage);
            NextPageCommand = new RelayCommand(GoToNextPage, CanGoToNextPage);
            LastPageCommand = new RelayCommand(GoToLastPage, CanGoToLastPage);

            if (disciplina == null)
            {
                _disciplina = new Disciplina();
                Title = "Nova Disciplina";
                IsEditing = false;
            }
            else
            {
                _disciplina = disciplina;
                Title = "Editar Disciplina";
                IsEditing = true;
            }

            Nome = _disciplina.Nome;
            Cor = _disciplina.Cor;
            IsArquivada = _disciplina.Arquivado;

            // Carrega os assuntos da disciplina se estiver editando
            if (IsEditing && disciplina != null)
            {
                _ = Task.Run(async () => await CarregarAssuntosAsync());
            }
        }

        // Propriedades da Disciplina
        private string _nome = string.Empty;
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Nome
        {
            get => _nome;
            set
            {
                if (SetProperty(ref _nome, value, true))
                {
                    ValidateNomeAsync(value);
                }
            }
        }

        private string _cor = "#3498db";
        [Required(ErrorMessage = "A cor é obrigatória.")]
        [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Formato de cor inválido. Use #RRGGBB.")]
        public string Cor
        {
            get => _cor;
            set => SetProperty(ref _cor, value, true);
        }

        private bool _isArquivada;
        public bool IsArquivada
        {
            get => _isArquivada;
            set => SetProperty(ref _isArquivada, value);
        }

        /// <summary>
        /// Propriedade que retorna a disciplina atual com todas as suas propriedades calculadas.
        /// Usada para binding com as estatísticas dinâmicas na view.
        /// </summary>
        public Disciplina CurrentDisciplina
        {
            get
            {
                // Atualiza a disciplina com os valores atuais do ViewModel
                _disciplina.Nome = Nome;
                _disciplina.Cor = Cor;
                
                // Retorna a disciplina com as propriedades calculadas
                return _disciplina;
            }
        }


        // Methods para carregamento e gerenciamento de assuntos
        private async Task CarregarAssuntosAsync()
        {
            if (_disciplina?.Id > 0)
            {
                try
                {
                    IsCarregando = true;
                    var assuntos = await _assuntoService.ObterPorDisciplinaAsync(_disciplina.Id);
                    
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        // Atualiza a lista completa de assuntos
                        _todosAssuntos.Clear();
                        _todosAssuntos.AddRange(assuntos);
                        
                        // Atualiza a coleção de assuntos da disciplina para que as propriedades calculadas funcionem
                        _disciplina.Assuntos.Clear();
                        foreach (var assunto in assuntos)
                        {
                            _disciplina.Assuntos.Add(assunto);
                        }
                        
                        // Invalida o cache de progresso para recalcular
                        _disciplina.InvalidateProgressCache();
                        
                        // Notifica que as propriedades calculadas foram atualizadas
                        OnPropertyChanged(nameof(CurrentDisciplina));
                        
                        // Aplica o filtro atual
                        FiltrarAssuntos();
                    });
                }
                catch (Exception ex)
                {
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        NotificationService.Instance.ShowError("Erro ao Carregar", $"Erro ao carregar assuntos: {ex.Message}");
                    });
                }
                finally
                {
                    IsCarregando = false;
                }
            }
        }

        // Commands obrigatórios
        private void AdicionarAssunto()
        {
            try
            {
                // Se há filtros/pesquisa ativos, limpar para evitar que novo assunto desapareça
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    SearchText = ""; // Isso dispara OnSearchTextChanged e aguarda o timer
                    // Aguardar um pouco para o filtro ser aplicado antes de adicionar
                    System.Threading.Thread.Sleep(350); // 300ms do timer + margem
                }

                // TODO: Implementar navegação correta após corrigir NavigationService
                // Por enquanto, vamos simular a adição de um assunto vazio que será editado
                var novoAssunto = new Assunto 
                { 
                    DisciplinaId = _disciplina.Id,
                    Nome = "",
                    CadernoQuestoes = "",
                    Concluido = false
                };
                
                // Adiciona ao final da lista para aparecer na última página
                _todosAssuntos.Add(novoAssunto);
                _disciplina.Assuntos.Add(novoAssunto);
                _disciplina.InvalidateProgressCache();
                OnPropertyChanged(nameof(CurrentDisciplina));

                // Recalcula a paginação e navega para a última página para exibir o novo assunto
                FiltrarAssuntos();
                GoToLastPage();

                // Entra automaticamente no modo de edição inline do novo assunto
                IniciarEdicaoAssunto(novoAssunto);
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError("Erro ao Adicionar", $"Erro ao adicionar assunto: {ex.Message}");
            }
        }

        private void AdicionarAssuntosEmLote()
        {
            try
            {
                // Se há filtros/pesquisa ativos, limpar para evitar que novos assuntos desapareçam
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    SearchText = ""; // Isso dispara OnSearchTextChanged e aguarda o timer
                    // Aguardar um pouco para o filtro ser aplicado antes de adicionar
                    System.Threading.Thread.Sleep(350); // 300ms do timer + margem
                }

                // Buscar a MainWindow - primeiro tenta MainWindow, depois procura entre as janelas abertas
                Window? ownerWindow = null;
                
                // Tentar obter a MainWindow
                if (Application.Current != null)
                {
                    ownerWindow = Application.Current.MainWindow;
                }
                
                // Se não conseguir, procurar entre as janelas abertas (usar a primeira janela visível)
                if (ownerWindow == null && Application.Current != null)
                {
                    ownerWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
                }

                // Criar e configurar o diálogo antes de exibir
                var dialog = new Views.AdicionarAssuntosEmLoteDialog
                {
                    Title = "Adicionar Assuntos em Lote",
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = ownerWindow != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                    Owner = ownerWindow,
                    ResizeMode = ResizeMode.CanResize
                };

                // Exibir o diálogo
                var resultado = dialog.ShowDialog();

                // Processar resultado
                if (resultado == true && dialog.AssuntosParaAdicionar.Count > 0)
                {
                    // Adicionar os assuntos à lista
                    foreach (var nomeAssunto in dialog.AssuntosParaAdicionar)
                    {
                        var novoAssunto = new Assunto
                        {
                            DisciplinaId = _disciplina.Id,
                            Nome = nomeAssunto,
                            CadernoQuestoes = "",
                            Concluido = false
                        };
                        _todosAssuntos.Add(novoAssunto);
                        _disciplina.Assuntos.Add(novoAssunto);
                    }

                    // Atualizar cache e notificar mudanças
                    _disciplina.InvalidateProgressCache();
                    OnPropertyChanged(nameof(CurrentDisciplina));

                    // Recalcular paginação e navegar para última página
                    FiltrarAssuntos();
                    GoToLastPage();

                    // Notificar sucesso com Toast
                    NotificationService.Instance.ShowSuccess(
                        "Assuntos Adicionados",
                        $"{dialog.AssuntosParaAdicionar.Count} assunto(s) adicionado(s) com sucesso! Clique em 'Salvar' para persistir.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError("Erro ao Adicionar em Lote", $"Erro ao adicionar assuntos em lote: {ex.Message}");
            }
        }

        private void RemoverAssunto(Assunto? assunto)
        {
            if (assunto == null) return;

            try
            {
                // Buscar a MainWindow - primeiro tenta MainWindow, depois procura entre as janelas abertas
                Window? ownerWindow = null;
                
                // Tentar obter a MainWindow
                if (Application.Current != null)
                {
                    ownerWindow = Application.Current.MainWindow;
                }
                
                // Se não conseguir, procurar entre as janelas abertas (usar a primeira janela visível)
                if (ownerWindow == null && Application.Current != null)
                {
                    ownerWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
                }

                var dialog = new Views.RemoverAssuntoDialog(_disciplinaService, _assuntoService, assunto, _disciplina)
                {
                    Owner = ownerWindow,
                    WindowStartupLocation = ownerWindow != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
                };
                
                var resultado = dialog.ShowDialog();
                
                if (resultado == true)
                {
                    // Remove o assunto da lista atual
                    _todosAssuntos.Remove(assunto);
                    _disciplina.Assuntos.Remove(assunto);
                    _disciplina.InvalidateProgressCache();
                    OnPropertyChanged(nameof(CurrentDisciplina));
                    FiltrarAssuntos();
                    
                    // Se o assunto já existe no banco (tem ID), marca para remoção com contexto
                    if (assunto.Id > 0 && dialog.ResultadoRemocao != null)
                    {
                        // Armazenar o contexto completo da remoção
                        _remocoesComContexto.Add(dialog.ResultadoRemocao);
                        
                        // Se for movimentação de estudos, preparar para mover
                        if (!dialog.ResultadoRemocao.RemoverEmCascata && 
                            dialog.ResultadoRemocao.AssuntoDestinoId.HasValue &&
                            dialog.ResultadoRemocao.DisciplinaDestinoId.HasValue)
                        {
                            _assuntosParaMover.Add((assunto, dialog.ResultadoRemocao.DisciplinaDestinoId.Value));
                        }
                        
                        // Sempre marcar para remoção
                        _assuntosParaRemover.Add(assunto);
                    }

                    NotificationService.Instance.ShowSuccess(
                        "Remoção Marcada",
                        dialog.ResultadoRemocao?.RemoverEmCascata == true
                            ? $"Assunto '{assunto.Nome}' e seus {dialog.ResultadoRemocao.TotalEstudos} estudo(s) marcado(s) para remoção em cascata! Clique em 'Salvar' para confirmar."
                            : $"Assunto '{assunto.Nome}' marcado para remoção e {dialog.ResultadoRemocao?.TotalEstudos ?? 0} estudo(s) para movimentação! Clique em 'Salvar' para confirmar.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError("Erro ao Remover", $"Erro ao remover assunto: {ex.Message}");
            }
        }

        private void AbrirAssunto(Assunto? assunto)
        {
            if (assunto == null) return;
            
            try
            {
                // TODO: Implementar navegação correta após corrigir NavigationService
                NotificationService.Instance.ShowInfo("Informação", $"Editar assunto: {assunto.Nome}\n\nFuncionalidade em desenvolvimento.");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError("Erro ao Editar", $"Erro ao editar assunto: {ex.Message}");
            }
        }

        private void MoverAssunto(Assunto? assunto)
        {
            if (assunto == null) return;

            try
            {
                // Buscar a MainWindow - primeiro tenta MainWindow, depois procura entre as janelas abertas
                Window? ownerWindow = null;
                
                // Tentar obter a MainWindow
                if (Application.Current != null)
                {
                    ownerWindow = Application.Current.MainWindow;
                }
                
                // Se não conseguir, procurar entre as janelas abertas (usar a primeira janela visível)
                if (ownerWindow == null && Application.Current != null)
                {
                    ownerWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
                }

                var dialog = new Views.MoverAssuntoDialog(_disciplinaService, _assuntoService, assunto, _disciplina)
                {
                    Owner = ownerWindow,
                    WindowStartupLocation = ownerWindow != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
                };
                
                var resultado = dialog.ShowDialog();
                
                if (resultado == true)
                {
                    // Remove o assunto da lista atual, pois foi movido
                    AssuntosSelecionados.Remove(assunto);
                    
                    // Notificar sucesso com Toast
                    //NotificationService.Instance.ShowSuccess(
                    //    "Movimentação Concluída",
                    //    "Assunto movido com sucesso! A lista foi atualizada.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError("Erro ao Mover", $"Erro ao mover assunto: {ex.Message}");
            }
        }

        private void IniciarEdicaoAssunto(Assunto? assunto)
        {
            if (assunto == null) return;

            try
            {
                // Sair do modo de edição de qualquer outro assunto (sem salvar)
                if (_assuntoEmEdicao != null)
                {
                    _assuntoEmEdicao.IsEditing = false;
                }
                
                // Entrar no modo de edição para este assunto
                assunto.IsEditing = true;
                _assuntoEmEdicao = assunto;
                
                // Rastrear que este assunto foi modificado (para otimizar salvamento)
                if (assunto.Id > 0)
                {
                    _assuntosModificados.Add(assunto.Id);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError("Erro ao Iniciar Edição", $"Erro ao iniciar edição: {ex.Message}");
            }
        }

        private void CancelarEdicaoAssunto(Assunto? assunto)
        {
            if (assunto == null) return;

            try
            {
                // Sair do modo de edição sem salvar alterações
                assunto.IsEditing = false;
                _assuntoEmEdicao = null;
                
                // Recarregar os dados originais do assunto (descartar alterações)
                // Como estamos trabalhando com objetos em memória, as alterações já foram feitas
                // Para uma implementação completa, seria necessário manter uma cópia dos dados originais
                // ou recarregar do banco de dados
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError("Erro ao Cancelar Edição", $"Erro ao cancelar edição: {ex.Message}");
            }
        }

        private void ToggleEditarAssunto(Assunto? assunto)
        {
            if (assunto == null) return;

            try
            {
                // Este método apenas sai do modo de edição sem salvar no banco
                // As alterações são mantidas em memória e serão persistidas quando o usuário clicar em "Salvar" no cabeçalho
                if (assunto.IsEditing)
                {
                    assunto.IsEditing = false;
                    _assuntoEmEdicao = null;
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError("Erro ao Sair da Edição", $"Erro ao sair do modo de edição: {ex.Message}");
            }
        }


        // Método chamado automaticamente quando SearchText muda
        partial void OnSearchTextChanged(string value)
        {
            // Implementar pesquisa com debounce
            CurrentPage = 1;
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void FiltrarAssuntos()
        {
            // Filtra assuntos baseado no texto de pesquisa
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                _assuntosFiltrados = _todosAssuntos.ToList();
            }
            else
            {
                _assuntosFiltrados = _todosAssuntos
                    .Where(a => a.Nome.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Atualiza contadores
            FilteredCount = _assuntosFiltrados.Count;
            TotalPages = (int)Math.Ceiling((double)FilteredCount / ItemsPerPage);
            
            // Garante que a página atual seja válida
            if (CurrentPage > TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }
            else if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            // Aplica paginação
            AtualizarPaginaAtual();

            // Notificar comandos de paginação para atualizar seu estado (enabled/disabled)
            ((RelayCommand)FirstPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)PreviousPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)NextPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)LastPageCommand).NotifyCanExecuteChanged();
        }

        private void AtualizarPaginaAtual()
        {
            AssuntosSelecionados.Clear();

            if (_assuntosFiltrados.Any())
            {
                var skip = (CurrentPage - 1) * ItemsPerPage;
                var assuntosPagina = _assuntosFiltrados
                    .Skip(skip)
                    .Take(ItemsPerPage)
                    .ToList();

                foreach (var assunto in assuntosPagina)
                {
                    AssuntosSelecionados.Add(assunto);
                }
            }
        }

        // Predicados para paginação
        private bool CanGoToFirstPage() => CurrentPage > 1;
        private bool CanGoToPreviousPage() => CurrentPage > 1;
        private bool CanGoToNextPage() => CurrentPage < TotalPages;
        private bool CanGoToLastPage() => CurrentPage < TotalPages;

        // Métodos de navegação de página
        private void GoToFirstPage()
        {
            CurrentPage = 1;
            AtualizarPaginaAtual();
            // Notificar que o estado dos comandos mudou
            ((RelayCommand)FirstPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)PreviousPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)NextPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)LastPageCommand).NotifyCanExecuteChanged();
        }

        private void GoToPreviousPage()
        {
            CurrentPage--;
            AtualizarPaginaAtual();
            // Notificar que o estado dos comandos mudou
            ((RelayCommand)FirstPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)PreviousPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)NextPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)LastPageCommand).NotifyCanExecuteChanged();
        }

        private void GoToNextPage()
        {
            CurrentPage++;
            AtualizarPaginaAtual();
            // Notificar que o estado dos comandos mudou
            ((RelayCommand)FirstPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)PreviousPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)NextPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)LastPageCommand).NotifyCanExecuteChanged();
        }

        private void GoToLastPage()
        {
            CurrentPage = TotalPages;
            AtualizarPaginaAtual();
            // Notificar que o estado dos comandos mudou
            ((RelayCommand)FirstPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)PreviousPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)NextPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)LastPageCommand).NotifyCanExecuteChanged();
        }

        private async Task SalvarAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔵 SalvarAsync() INICIADO - Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            
            if (_disciplinaService == null || _navigationService == null || _transactionService == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ SalvarAsync() - Serviços nulos");
                return;
            }

            ValidateAllProperties();
            if (HasErrors)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ SalvarAsync() - Erros de validação encontrados");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🟡 SalvarAsync() - Definindo IsSaving = true");
                IsSaving = true;

                // Permitir que a UI renderize o ícone de loading antes de iniciar a operação
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ⏳ SalvarAsync() - Aguardando Dispatcher.BeginInvoke para renderização da UI");
                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ SalvarAsync() - Dispatcher.BeginInvoke executado, UI renderizada");
                    tcs.SetResult(true);
                }), System.Windows.Threading.DispatcherPriority.Render);
                await tcs.Task;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ SalvarAsync() - Dispatcher.BeginInvoke completado, iniciando operações");

                // Atualiza os dados da disciplina
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 📝 SalvarAsync() - Atualizando dados da disciplina: Nome={Nome}, Cor={Cor}, Arquivado={IsArquivada}");
                _disciplina.Nome = Nome;
                _disciplina.Cor = Cor;
                _disciplina.Arquivado = IsArquivada;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ SalvarAsync() - Dados da disciplina atualizados");

                // Preparar lista de assuntos modificados (apenas novos e editados)
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 📊 SalvarAsync() - Preparando lista de assuntos modificados");
                var assuntosModificadosParaSalvar = _todosAssuntos
                    .Where(a => a.Id == 0 || _assuntosModificados.Contains(a.Id))
                    .ToList();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 📊 SalvarAsync() - Total de assuntos para salvar: {assuntosModificadosParaSalvar.Count} (novos + editados)");

                // Usar transação para salvar disciplina e assuntos atomicamente
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔄 SalvarAsync() - Iniciando transação única");
                await _transactionService.SalvarDisciplinaComAssuntosAsync(
                    _disciplina,
                    !IsEditing,  // isNovaDisciplina
                    assuntosModificadosParaSalvar,
                    _assuntosParaRemover,
                    _assuntosParaMover,
                    _remocoesComContexto);  // Passar contexto de remoções
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ SalvarAsync() - Transação concluída com sucesso");

                // Limpar rastreamento de modificações
                _assuntosModificados.Clear();
                _assuntosParaRemover.Clear();
                _assuntosParaMover.Clear();
                _remocoesComContexto.Clear();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🎉 SalvarAsync() - Exibindo notificação de sucesso");
                NotificationService.Instance.ShowSuccess("Sucesso", "Disciplina salva com sucesso!");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔙 SalvarAsync() - Navegando para trás");
                _navigationService.GoBack();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ SalvarAsync() - Navegação concluída");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💥 SalvarAsync() - EXCEÇÃO: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💥 Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💥 StackTrace: {ex.StackTrace}");
                NotificationService.Instance.ShowError("Erro ao Salvar", $"Erro ao salvar a disciplina: {ex.Message}");
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🔴 SalvarAsync() - Finally: Definindo IsSaving = false");
                IsSaving = false;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ SalvarAsync() - IsSaving definido como false");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 🏁 SalvarAsync() FINALIZADO - Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            }
        }

        private void Cancelar()
        {
            _navigationService?.GoBack();
        }

        private void ValidateNomeAsync(string nome)
        {
            if (_disciplinaService == null) return;

            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();
            var token = _debounceTokenSource.Token;

            Task.Delay(500, token).ContinueWith(async t =>
            {
                if (t.IsCanceled) return;

                ClearErrors(nameof(Nome));
                if (string.IsNullOrWhiteSpace(nome)) return;

                bool exists = await _disciplinaService.NomeExistsAsync(nome, _disciplina.Id > 0 ? _disciplina.Id : null);
                if (exists)
                {
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        AddError(nameof(Nome), "Este nome de disciplina já está em uso.");
                    });
                }
            }, token);
        }

        private void AddError(string propertyName, string errorMessage)
        {
            if (!_validationErrors.ContainsKey(propertyName))
            {
                _validationErrors[propertyName] = new List<string>();
            }
            
            if (!_validationErrors[propertyName].Contains(errorMessage))
            {
                _validationErrors[propertyName].Add(errorMessage);
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            if (e.PropertyName != null && _validationErrors.ContainsKey(e.PropertyName))
            {
                _validationErrors.Remove(e.PropertyName);
            }
        }

        public new System.Collections.IEnumerable GetErrors(string? propertyName)
        {
            var allErrors = new List<string>();
            
            var baseErrors = base.GetErrors(propertyName);
            if (baseErrors != null)
            {
                foreach (var error in baseErrors)
                {
                    var validationResult = error as ValidationResult;
                    if (validationResult?.ErrorMessage != null)
                    {
                        allErrors.Add(validationResult.ErrorMessage);
                    }
                }
            }
            
            if (propertyName != null && _validationErrors.ContainsKey(propertyName))
            {
                allErrors.AddRange(_validationErrors[propertyName]);
            }
            
            return allErrors;
        }

        // Método para selecionar cor da paleta
        private void SelecionarCor(string? hexColor)
        {
            if (!string.IsNullOrEmpty(hexColor))
            {
                Cor = hexColor;
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
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                FiltrarAssuntos();
                // Notificar comandos de paginação para atualizar seu estado
                ((RelayCommand)FirstPageCommand).NotifyCanExecuteChanged();
                ((RelayCommand)PreviousPageCommand).NotifyCanExecuteChanged();
                ((RelayCommand)NextPageCommand).NotifyCanExecuteChanged();
                ((RelayCommand)LastPageCommand).NotifyCanExecuteChanged();
            });
        }

        /// <summary>
        /// Propriedade para rastrear se há alterações não salvas
        /// </summary>
        public bool HasUnsavedChanges
        {
            get
            {
                if (_disciplina == null)
                    return false;

                // Verificar se a disciplina foi modificada
                bool disciplinaModificada = 
                    Nome?.Trim() != _disciplina.Nome?.Trim() ||
                    Cor != _disciplina.Cor ||
                    IsArquivada != _disciplina.Arquivado;

                // Verificar se há assuntos modificados
                bool assuntosModificados = _assuntosModificados.Count > 0;

                return disciplinaModificada || assuntosModificados;
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

            var resultado = NotificationService.Instance.ShowConfirmation(
                "Alterações Não Salvas",
                "Você tem alterações não salvas. Deseja descartá-las?");

            return resultado != ToastMessageBoxResult.Yes;
        }
    }
}
