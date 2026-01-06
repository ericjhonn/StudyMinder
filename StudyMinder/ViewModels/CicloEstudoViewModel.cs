using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Navigation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using StudyMinder.Views;

namespace StudyMinder.ViewModels
{
    public partial class CicloEstudoViewModel : BaseViewModel
    {
        private readonly CicloEstudoService _cicloEstudoService;
        private readonly NavigationService _navigationService;
        private readonly EstudoService _estudoService;
        private readonly TipoEstudoService _tipoEstudoService;
        private readonly AssuntoService _assuntoService;
        private readonly DisciplinaService _disciplinaService;
        private readonly EstudoTransactionService _transactionService;
        private readonly INotificationService _notificationService;
        private readonly IConfigurationService _configurationService;
        private readonly RevisaoService _revisaoService;

        private Dictionary<int, AssuntoSelecionavel> _selecoesAssuntos = new Dictionary<int, AssuntoSelecionavel>();

        [ObservableProperty]
        private ObservableCollection<CicloEstudo> _ciclo = new();

        [ObservableProperty]
        private ObservableCollection<AssuntoSelecionavel> _assuntosDisponiveis = new();

        [ObservableProperty]
        private CicloEstudo? _proximoSugerido;

        [ObservableProperty]
        private bool _isEditing = false;

        [ObservableProperty]
        private bool _isCarregando = false;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _paginaAtual = 1;

        [ObservableProperty]
        private int _totalPaginas = 1;

        [ObservableProperty]
        private int _itensPorPagina = 15;

        [ObservableProperty]
        private int _totalItens = 0;

        [ObservableProperty]
        private int _filteredCount = 0;

        public CicloEstudoViewModel(
            CicloEstudoService cicloEstudoService,
            NavigationService navigationService,
            EstudoService estudoService,
            TipoEstudoService tipoEstudoService,
            AssuntoService assuntoService,
            DisciplinaService disciplinaService,
            EstudoTransactionService transactionService,
            INotificationService notificationService,
            IConfigurationService configurationService,
            RevisaoService revisaoService)
        {
            Title = "Ciclo de Estudo";

            _cicloEstudoService = cicloEstudoService;
            _navigationService = navigationService;
            _estudoService = estudoService;
            _tipoEstudoService = tipoEstudoService;
            _assuntoService = assuntoService;
            _disciplinaService = disciplinaService;
            _transactionService = transactionService;
            _notificationService = notificationService;
            _configurationService = configurationService;
            _revisaoService = revisaoService;

            _navigationService.Navigated += OnNavigated;
        }

        private async void OnNavigated(object? sender, object page)
        {
            if (page is ViewCicloEstudo)
            {
                await CarregarDadosAsync();
            }
        }

        private async Task CarregarDadosAsync()
        {
            IsCarregando = true;
            try
            {
                var cicloTask = _cicloEstudoService.ObterCicloAsync();
                var proximoTask = _cicloEstudoService.ObterProximoSugestaoAsync();

                await Task.WhenAll(cicloTask, proximoTask);

                Ciclo = new ObservableCollection<CicloEstudo>(cicloTask.Result);
                ProximoSugerido = proximoTask.Result;
                FilteredCount = Ciclo.Count;
            }
            catch (System.Exception ex)
            {
                _notificationService.ShowError("Erro", $"Não foi possível carregar o ciclo de estudos: {ex.Message}");
            }
            finally
            {
                IsCarregando = false;
            }
        }

        [RelayCommand]
        private async Task MoverParaCima(CicloEstudo cicloItem)
        {
            if (cicloItem == null) return;
            await _cicloEstudoService.MoverItemAsync(cicloItem.AssuntoId, true);
            await CarregarDadosAsync();
        }

        [RelayCommand]
        private async Task MoverParaBaixo(CicloEstudo cicloItem)
        {
            if (cicloItem == null) return;
            await _cicloEstudoService.MoverItemAsync(cicloItem.AssuntoId, false);
            await CarregarDadosAsync();
        }

        [RelayCommand]
        private async Task RemoverItem(int assuntoId)
        {
            await _cicloEstudoService.RemoverDoCicloAsync(assuntoId);
            await CarregarDadosAsync();
            _notificationService.ShowSuccess("Sucesso", "Assunto removido do ciclo.");
        }

        [RelayCommand]
        private async Task IniciarEstudo(CicloEstudo? cicloEstudo)
        {
            if (cicloEstudo?.Assunto == null) return;

            var viewModel = new EditarEstudoViewModel(_estudoService, _tipoEstudoService, _assuntoService, _disciplinaService, _transactionService, _navigationService, _revisaoService, _notificationService, _configurationService);
            
            var ultimoEstudo = await _estudoService.ObterUltimoEstudoPorAssuntoAsync(cicloEstudo.AssuntoId);
            int? pagina = ultimoEstudo?.PaginaFinal;

            await viewModel.InicializarComAssuntoAsync(cicloEstudo.Assunto, pagina, 0);

            var view = new ViewEstudoEditar { DataContext = viewModel };
            _navigationService.NavigateTo(view);
        }

        [RelayCommand]
        private async Task EstudarCiclo(CicloEstudo cicloEstudo)
        {
            await IniciarEstudo(cicloEstudo);
        }

        [RelayCommand]
        private async Task AlternarModoEdicao()
        {
            if (!IsEditing)
            {
                // ENTRAR NO MODO EDIÇÃO
                IsEditing = true;
                PaginaAtual = 1;

                // Importante: Limpar seleções anteriores para começar "fresco"
                _selecoesAssuntos.Clear();

                await CarregarAssuntosDisponiveisAsync();
            }
            else
            {
                // SAIR DO MODO EDIÇÃO (SALVAR)
                await SalvarEdicao();
            }
        }

        [RelayCommand]
        private async Task SalvarEdicao()
        {
            IsCarregando = true;

            try
            {
                // 1. IDENTIFICAR O QUE ADICIONAR
                // Assuntos marcados (IsSelected) que NÃO estão no Ciclo atual
                var assuntosParaAdicionar = _selecoesAssuntos.Values
                    .Where(a => a.IsSelected && !Ciclo.Any(c => c.AssuntoId == a.Assunto.Id))
                    .ToList();

                // 2. IDENTIFICAR O QUE REMOVER
                // Itens do Ciclo atual que foram desmarcados pelo utilizador.
                // Nota: Só verificamos itens presentes em _selecoesAssuntos (ou seja, que foram carregados/vistos).
                // Se o utilizador não carregou a página de um assunto, assumimos que não quis alterá-lo.
                var assuntosParaRemover = Ciclo
                    .Where(c => _selecoesAssuntos.ContainsKey(c.AssuntoId) && !_selecoesAssuntos[c.AssuntoId].IsSelected)
                    .ToList();

                // 3. PERSISTIR ADIÇÕES
                foreach (var item in assuntosParaAdicionar)
                {
                    // Adiciona com tempo padrão de 60 min (pode ser ajustado depois na lista visual)
                    await _cicloEstudoService.AdicionarAoCicloAsync(item.Assunto.Id, 60);
                }

                // 4. PERSISTIR REMOÇÕES
                foreach (var item in assuntosParaRemover)
                {
                    await _cicloEstudoService.RemoverDoCicloAsync(item.AssuntoId);
                }

                // 5. FINALIZAR
                if (assuntosParaAdicionar.Any() || assuntosParaRemover.Any())
                {
                    _notificationService.ShowSuccess("Sucesso", "Ciclo de estudos atualizado.");
                }
                else
                {
                    // Opcional: Feedback se nada mudou
                    // _notificationService.ShowInfo("Info", "Nenhuma alteração realizada.");
                }

                // Sair do modo edição e recarregar a lista principal (Ciclo) do banco
                IsEditing = false;
                _selecoesAssuntos.Clear(); // Limpa memória
                await CarregarDadosAsync(); // Atualiza a lista visual do ciclo e a sugestão
            }
            catch (System.Exception ex)
            {
                _notificationService.ShowError("Erro", $"Falha ao salvar alterações: {ex.Message}");
            }
            finally
            {
                IsCarregando = false;
            }
        }

        [RelayCommand]
        private void CancelarEdicao()
        {
            IsEditing = false;
            _selecoesAssuntos.Clear();
        }
        
        [RelayCommand]
        private async Task AtualizarDuracao(CicloEstudo cicloItem)
        {
            if (cicloItem == null) return;
            await _cicloEstudoService.AtualizarDuracaoAsync(cicloItem.AssuntoId, cicloItem.DuracaoMinutos);
        }

        private async Task CarregarAssuntosDisponiveisAsync()
        {
            IsCarregando = true;

            // Busca a página atual contendo assuntos misturados (do ciclo e fora dele)
            var resultado = await _cicloEstudoService.ObterAssuntosDisponiveisPaginadoAsync(PaginaAtual, ItensPorPagina, SearchText);

            Application.Current.Dispatcher.Invoke(() =>
            {
                AssuntosDisponiveis.Clear();

                // Criamos um HashSet dos IDs que estão atualmente no Ciclo para verificação rápida
                var idsNoCiclo = Ciclo.Select(c => c.AssuntoId).ToHashSet();

                foreach (var assunto in resultado.Items)
                {
                    bool isSelected = false;

                    // LÓGICA DE SELEÇÃO:
                    // 1. Se já manipulamos este item nesta sessão de edição (está no dicionário), respeitamos a escolha do usuário.
                    if (_selecoesAssuntos.ContainsKey(assunto.Id))
                    {
                        isSelected = _selecoesAssuntos[assunto.Id].IsSelected;
                    }
                    // 2. Caso contrário, verificamos se ele já existe no Ciclo salvo no banco.
                    else
                    {
                        isSelected = idsNoCiclo.Contains(assunto.Id);
                    }

                    // Criamos o objeto visual
                    var selecionavel = new AssuntoSelecionavel(assunto, isSelected);

                    // Adicionamos à lista visual (agora ordenada alfabeticamente junto com os outros)
                    AssuntosDisponiveis.Add(selecionavel);

                    // Garantimos que o dicionário de rastreamento esteja atualizado
                    if (!_selecoesAssuntos.ContainsKey(assunto.Id))
                    {
                        _selecoesAssuntos[assunto.Id] = selecionavel;
                    }
                    else
                    {
                        // Se já existia (ex: voltando de outra página), atualizamos a referência visual
                        _selecoesAssuntos[assunto.Id] = selecionavel;
                    }
                }

                // Atualiza contadores
                TotalItens = resultado.TotalCount;
                TotalPaginas = (int)System.Math.Ceiling((double)TotalItens / ItensPorPagina);
                FilteredCount = AssuntosDisponiveis.Count;
            });

            IsCarregando = false;
        }

        partial void OnSearchTextChanged(string value)
        {
            if (IsEditing)
            {
                PaginaAtual = 1;
                CarregarAssuntosDisponiveisAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
        private async Task FirstPage()
        {
            if (CanGoToFirstPage())
            {
                PaginaAtual = 1;
                await CarregarAssuntosDisponiveisAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        private async Task PreviousPage()
        {
            if (CanGoToPreviousPage())
            {
                PaginaAtual--;
                await CarregarAssuntosDisponiveisAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        private async Task NextPage()
        {
            if (CanGoToNextPage())
            {
                PaginaAtual++;
                await CarregarAssuntosDisponiveisAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
        private async Task LastPage()
        {
            if (CanGoToLastPage())
            {
                PaginaAtual = TotalPaginas;
                await CarregarAssuntosDisponiveisAsync();
            }
        }

        private bool CanGoToFirstPage() => PaginaAtual > 1;
        private bool CanGoToPreviousPage() => PaginaAtual > 1;
        private bool CanGoToNextPage() => PaginaAtual < TotalPaginas;
        private bool CanGoToLastPage() => PaginaAtual < TotalPaginas;

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
    }
}
