using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using StudyMinder.Data;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Utils;
using StudyMinder.Views;
using StudyMinder.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace StudyMinder.ViewModels
{
    public partial class CalendarioViewModel : BaseViewModel, IRefreshable
    {
        private readonly StudyMinderContext _context;
        private readonly EstudoService _estudoService;
        private readonly TipoEstudoService _tipoEstudoService;
        private readonly AssuntoService _assuntoService;
        private readonly DisciplinaService _disciplinaService;
        private readonly EstudoTransactionService _transactionService;
        private readonly NavigationService _navigationService;
        private readonly RevisaoService _revisaoService;
        private readonly INotificationService _notificationService;
        private readonly IConfigurationService _configurationService;

        private Dictionary<DateTime, EventosDia> _cacheEventos = new();
        private DateTime _dataInicioCarregada = DateTime.MaxValue;
        private DateTime _dataFimCarregada = DateTime.MinValue;

        private const int TIMEOUT_SECONDS = 30;

        [ObservableProperty]
        private DateTime _dataExibida;

        [ObservableProperty]
        private bool _isVisualizacaoSemanal = false;

        [ObservableProperty]
        private bool _isAgendaViewAtiva = false;

        [ObservableProperty]
        private EventosDia? _diaSelecionado;

        [ObservableProperty]
        private string _mensagemErro = string.Empty;

        [ObservableProperty]
        private bool _temErro = false;

        public ObservableCollection<object> AgendaEventos { get; } = new();
        public event Action? RequestRender;

        public CalendarioViewModel(
            StudyMinderContext context,
            EstudoNotificacaoService estudoNotificacaoService,
            EditalCronogramaNotificacaoService editalCronogramaNotificacaoService,
            EstudoService estudoService,
            TipoEstudoService tipoEstudoService,
            AssuntoService assuntoService,
            DisciplinaService disciplinaService,
            EstudoTransactionService transactionService,
            NavigationService navigationService,
            RevisaoService revisaoService,
            INotificationService notificationService,
            IConfigurationService configurationService)
        {
            _context = context;
            _estudoService = estudoService;
            _tipoEstudoService = tipoEstudoService;
            _assuntoService = assuntoService;
            _disciplinaService = disciplinaService;
            _transactionService = transactionService;
            _navigationService = navigationService;
            _revisaoService = revisaoService;
            _notificationService = notificationService;
            _configurationService = configurationService;

            Title = "Calendário de Estudos";
            _dataExibida = DateTime.Today;

            MoverEventoCommand = new RelayCommand<EditalCronograma?>(MoverEvento);
            EditarEstudoCommand = new RelayCommand<Estudo?>(EditarEstudo);
            IniciarRevisaoCommand = new RelayCommand<Revisao?>(IniciarRevisao);

            estudoNotificacaoService.EstudoAdicionado += OnModelChanged;
            estudoNotificacaoService.EstudoAtualizado += OnModelChanged;
            estudoNotificacaoService.EstudoRemovido += OnModelChanged;

            editalCronogramaNotificacaoService.CronogramaAdicionado += OnModelChanged;
            editalCronogramaNotificacaoService.CronogramaAtualizado += OnModelChanged;
            editalCronogramaNotificacaoService.CronogramaRemovido += OnModelChanged;
        }

        partial void OnDataExibidaChanged(DateTime value)
        {
            if (IsAgendaViewAtiva)
            {
                _ = CarregarAgendaAsync();
            }
            else
            {
                _ = CarregarDadosPeriodoAsync();
            }
        }

        private void OnModelChanged(object? sender, EventArgs e)
        {
            RefreshData();
        }

        [RelayCommand]
        private void FecharDetalhes() => DiaSelecionado = null;

        [RelayCommand]
        private void SelecionarDia(DateTime data)
        {
            DiaSelecionado = ObterEventosDiaOtimizado(data.Date);
        }

        [RelayCommand]
        private void MudarVisualizacao(string modo)
        {
            IsAgendaViewAtiva = modo == "Agenda";
            IsVisualizacaoSemanal = modo == "Semanal";
            
            // Força o recarregamento dos dados para a nova visualização
            RefreshData();
        }

        [RelayCommand]
        private void NavegarPeriodo(string direcao)
        {
            int step = direcao == "Proximo" ? 1 : -1;
            DataExibida = IsVisualizacaoSemanal
                ? DataExibida.AddDays(7 * step)
                : DataExibida.AddMonths(step);
        }

        [RelayCommand]
        private void IrParaHoje()
        {
            DataExibida = DateTime.Today;
        }

        public IRelayCommand<EditalCronograma?> MoverEventoCommand { get; }
        public IRelayCommand<Estudo?> EditarEstudoCommand { get; }
        public IRelayCommand<Revisao?> IniciarRevisaoCommand { get; }

        public async Task CarregarDadosPeriodoAsync()
        {
            if (IsBusy) return;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TIMEOUT_SECONDS));

            try
            {
                IsBusy = true;
                TemErro = false;

                DateTime dataInicio, dataFim;
                if (IsVisualizacaoSemanal)
                {
                    var inicioSemana = DateUtils.GetInicioSemana(DataExibida);
                    dataInicio = inicioSemana;
                    dataFim = inicioSemana.AddDays(7).AddTicks(-1);
                }
                else
                {
                    dataInicio = new DateTime(DataExibida.Year, DataExibida.Month, 1).AddMonths(-1);
                    dataFim = dataInicio.AddMonths(3).AddDays(-1);
                }

                if (_dataInicioCarregada <= dataInicio && _dataFimCarregada >= dataFim)
                {
                    RequestRender?.Invoke();
                    return;
                }

                var (estudos, eventos, revisoes) = await FetchData(dataInicio, dataFim, cts.Token);
                AtualizarCache(estudos, eventos, revisoes);
                
                _dataInicioCarregada = dataInicio;
                _dataFimCarregada = dataFim;

                RequestRender?.Invoke();
            }
            catch (OperationCanceledException)
            {
                TemErro = true;
                MensagemErro = "O carregamento dos dados demorou muito e foi cancelado.";
                RequestRender?.Invoke();
            }
            catch (Exception ex)
            {
                TemErro = true;
                MensagemErro = $"Ocorreu um erro ao carregar os dados: {ex.Message}";
                RequestRender?.Invoke();
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async Task CarregarAgendaAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var inicioMes = new DateTime(DataExibida.Year, DataExibida.Month, 1);
                var fimMes = inicioMes.AddMonths(1).AddDays(-1);

                var (estudos, eventos, revisoes) = await FetchData(inicioMes, fimMes, CancellationToken.None);

                var todosOsEventos = new List<object>();
                todosOsEventos.AddRange(estudos);
                todosOsEventos.AddRange(revisoes);
                todosOsEventos.AddRange(eventos);

                var sortedEvents = todosOsEventos.OrderBy(GetDateFromEvent).ToList();
                
                AgendaEventos.Clear();
                foreach (var ev in sortedEvents)
                {
                    AgendaEventos.Add(ev);
                }
            }
            catch(Exception ex)
            {
                TemErro = true;
                MensagemErro = $"Ocorreu um erro ao carregar a agenda: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private DateTime GetDateFromEvent(object obj)
        {
            return obj switch
            {
                Estudo e => e.Data,
                Revisao r => r.DataProgramada,
                EditalCronograma ec => ec.DataEvento,
                _ => throw new ArgumentException("Tipo de evento desconhecido")
            };
        }

        private async Task<(List<Estudo>, List<EditalCronograma>, List<Revisao>)> FetchData(DateTime de, DateTime ate, CancellationToken token)
        {
            var deTicks = de.Ticks;
            var ateTicks = ate.Ticks;

            var estudosTask = _context.Estudos.AsNoTracking().Include(e => e.Assunto).Include(e => e.Assunto.Disciplina)
                                      .Where(e => e.DataTicks >= deTicks && e.DataTicks <= ateTicks)
                                      .ToListAsync(token);

            var eventosTask = _context.EditalCronograma.AsNoTracking().Include(ec => ec.Edital)
                                      .Where(e => e.DataEventoTicks >= de.Ticks && e.DataEventoTicks <= ate.Ticks)
                                      .ToListAsync(token);

            var revisoesTask = _context.Revisoes.AsNoTracking().Include(r => r.EstudoOrigem.Assunto).Include(e => e.EstudoOrigem.Assunto.Disciplina)
                                       .Where(r => r.DataProgramadaTicks >= de.Ticks && r.DataProgramadaTicks <= ate.Ticks)
                                       .ToListAsync(token);

            // NOVA BUSCA: Editais para projeção de datas importantes
            var editaisTask = _context.Editais.AsNoTracking()
                                      .Where(e => !e.Arquivado)
                                      .ToListAsync(token);

            await Task.WhenAll(estudosTask, eventosTask, revisoesTask, editaisTask);

            var listaCronograma = await eventosTask;
            var listaEditais = await editaisTask;

            // Gerar e mesclar eventos virtuais (Prova, Abertura, etc)
            var eventosVirtuais = GerarEventosVirtuaisDeEditais(listaEditais, de, ate);
            listaCronograma.AddRange(eventosVirtuais);

            return (await estudosTask, listaCronograma, await revisoesTask);
        }

        private void AtualizarCache(List<Estudo> estudos, List<EditalCronograma> eventos, List<Revisao> revisoes)
        {
            _cacheEventos.Clear();

            foreach (var estudo in estudos)
            {
                var data = estudo.Data.Date;
                if (!_cacheEventos.ContainsKey(data)) _cacheEventos[data] = new EventosDia { Data = data };
                _cacheEventos[data].Estudos.Add(estudo);
            }
            
            foreach (var evento in eventos)
            {
                var data = evento.DataEvento.Date;
                if (!_cacheEventos.ContainsKey(data)) _cacheEventos[data] = new EventosDia { Data = data };
                _cacheEventos[data].EventosEditais.Add(evento);
            }
            
            foreach (var revisao in revisoes)
            {
                var data = revisao.DataProgramada.Date;
                if (!_cacheEventos.ContainsKey(data)) _cacheEventos[data] = new EventosDia { Data = data };
                _cacheEventos[data].Revisoes.Add(revisao);
            }
        }
        
        public Dictionary<DateTime, EventosDia> ObterCacheEventos() => _cacheEventos;
        
        public EventosDia ObterEventosDiaOtimizado(DateTime data)
        {
            return _cacheEventos.TryGetValue(data.Date, out var eventos) ? eventos : new EventosDia { Data = data.Date };
        }
        
        public void RefreshData()
        {
            _dataInicioCarregada = DateTime.MaxValue;
            _dataFimCarregada = DateTime.MinValue;
            if (IsAgendaViewAtiva)
            {
                _ = CarregarAgendaAsync();
            }
            else
            {
                _ = CarregarDadosPeriodoAsync();
            }
        }

        // Gera os eventos virtuais (não persistidos) baseados nas datas do Edital
        private List<EditalCronograma> GerarEventosVirtuaisDeEditais(List<Edital> editais, DateTime inicioPeriodo, DateTime fimPeriodo)
        {
            var eventosVirtuais = new List<EditalCronograma>();
            long inicioTicks = inicioPeriodo.Ticks;
            long fimTicks = fimPeriodo.Ticks;

            foreach (var edital in editais)
            {
                // 1. Data da Prova
                if (edital.DataProvaTicks >= inicioTicks && edital.DataProvaTicks <= fimTicks)
                {
                    eventosVirtuais.Add(new EditalCronograma
                    {
                        Id = -1, // ID negativo indica "Virtual"
                        EditalId = edital.Id,
                        Edital = edital,
                        Evento = $"{edital.Orgao} - {edital.Cargo}",
                        DataEventoTicks = edital.DataProvaTicks
                    });
                }

                // 2. Data de Abertura
                if (edital.DataAberturaTicks >= inicioTicks && edital.DataAberturaTicks <= fimTicks)
                {
                    eventosVirtuais.Add(new EditalCronograma
                    {
                        Id = -1,
                        EditalId = edital.Id,
                        Edital = edital,
                        Evento = "Publicação do Edital",
                        DataEventoTicks = edital.DataAberturaTicks
                    });
                }

                // 3. Homologação
                if (edital.DataHomologacaoTicks.HasValue &&
                    edital.DataHomologacaoTicks.Value >= inicioTicks &&
                    edital.DataHomologacaoTicks.Value <= fimTicks)
                {
                    eventosVirtuais.Add(new EditalCronograma
                    {
                        Id = -1,
                        EditalId = edital.Id,
                        Edital = edital,
                        Evento = "Mologação",
                        DataEventoTicks = edital.DataHomologacaoTicks.Value
                    });
                }

                // 4. Fim da Validade (Calculada)
                if (edital.DataHomologacaoTicks.HasValue && edital.Validade > 0)
                {
                    var dataFimValidade = new DateTime(edital.DataHomologacaoTicks.Value).AddMonths((int)edital.Validade);
                    if (dataFimValidade.Ticks >= inicioTicks && dataFimValidade.Ticks <= fimTicks)
                    {
                        eventosVirtuais.Add(new EditalCronograma
                        {
                            Id = -1,
                            EditalId = edital.Id,
                            Edital = edital,
                            Evento = "Fim da Validade",
                            DataEventoTicks = dataFimValidade.Ticks
                        });
                    }
                }
            }

            return eventosVirtuais;
        }

        // Lógica para editar um estudo
        private void EditarEstudo(Estudo? estudo)
        {
            // 1. Validação de tipo
            if (estudo is null) return;

            try
            {
                // Usar a mesma lógica de edição do EstudosViewModel
                var viewModel = new EditarEstudoViewModel(
                    _estudoService,
                    _tipoEstudoService,
                    _assuntoService,
                    _disciplinaService,
                    _transactionService,
                    _navigationService,
                    _revisaoService,
                    _notificationService,
                    _configurationService,
                    estudo);

                viewModel.EstudoSalvo += async (s, e) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        // Recarregar dados e fechar o painel de detalhes
                        await Task.Run(() => RefreshData());
                        DiaSelecionado = null;
                    });
                };

                var view = new Views.ViewEstudoEditar { DataContext = viewModel };
                _navigationService.NavigateTo(view);
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao editar estudo: {ex.Message}";
                TemErro = true;
            }
        }

        // Lógica para iniciar uma revisão
        private async void IniciarRevisao(Revisao? revisao)
        {
            // 1. Validação de tipo
            if (revisao is null) return;

            try
            {
                IsBusy = true; // Indica carregamento na UI

                // 2. Garantir que temos o ID do assunto
                // A revisão está ligada a um EstudoOrigem, que está ligado ao Assunto
                if (revisao.EstudoOrigem == null)
                {
                    // Tenta carregar do banco se o Include falhou (segurança)
                    var revisaoCompleta = await _context.Revisoes
                        .Include(r => r.EstudoOrigem)
                        .FirstOrDefaultAsync(r => r.Id == revisao.Id);

                    if (revisaoCompleta?.EstudoOrigem == null)
                    {
                        _notificationService.ShowError("Erro", "Não foi possível identificar o estudo de origem desta revisão.");
                        return;
                    }
                    revisao = revisaoCompleta;
                }

                int assuntoId = revisao.EstudoOrigem.AssuntoId;

                // 3. Obter dados completos do Assunto e Disciplina (Padrão do HomeViewModel)
                // Buscamos via serviço para garantir objetos "frescos" e completos para a edição
                var assuntos = await _assuntoService.ObterTodosAsync();
                var assuntoCompleto = assuntos.FirstOrDefault(a => a.Id == assuntoId);

                if (assuntoCompleto == null)
                {
                    _notificationService.ShowError("Erro", "Assunto não encontrado.");
                    return;
                }

                var disciplinas = await _disciplinaService.ObterTodasAsync();
                var disciplina = disciplinas.FirstOrDefault(d => d.Id == assuntoCompleto.DisciplinaId);

                if (disciplina == null)
                {
                    _notificationService.ShowError("Erro", "Disciplina não encontrada.");
                    return;
                }

                // 4. Carregar tipo de estudo "Revisão"
                var tipoRevisao = await _estudoService.ObterTipoEstudoPorNomeAsync("Revisão");
                if (tipoRevisao == null)
                {
                    // Fallback: tenta pegar o primeiro tipo se "Revisão" não existir
                    var tipos = await _tipoEstudoService.ObterAtivosAsync();
                    tipoRevisao = tipos.FirstOrDefault();

                    if (tipoRevisao == null)
                    {
                        _notificationService.ShowError("Configuração", "Nenhum Tipo de Estudo 'Revisão' encontrado.");
                        return;
                    }
                }

                // 5. Criar ViewModel de Edição (Igual à Home)
                var viewModel = new EditarEstudoViewModel(
                    _estudoService,
                    _tipoEstudoService,
                    _assuntoService,
                    _disciplinaService,
                    _transactionService,
                    _navigationService,
                    _revisaoService,
                    _notificationService,
                    _configurationService); // Se faltar parâmetro 'estudo', o construtor correto é o sem parâmetros de estudo

                // 6. Configurar Callback de Salvamento
                viewModel.EstudoSalvo += async (sender, args) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        // Fecha o painel de detalhes
                        DiaSelecionado = null;

                        // Recarrega o calendário para mostrar a revisão como concluída (check verde)
                        await Task.Run(() => RefreshData());

                        System.Diagnostics.Debug.WriteLine("[DEBUG] Revisão concluída via Calendário. Dados recarregados.");
                    });
                };

                // 7. Inicializar Modo Revisão
                // Passamos o ID da revisão para que o sistema saiba que deve dar baixa nela ao salvar
                await viewModel.InicializarModoRevisaoAsync(disciplina, assuntoCompleto, tipoRevisao, revisao.Id);

                // 8. Navegar
                DiaSelecionado = null; // Fecha o painel lateral antes de navegar
                var view = new Views.ViewEstudoEditar { DataContext = viewModel };
                _navigationService.NavigateTo(view);
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao iniciar revisão: {ex.Message}";
                TemErro = true;
                _notificationService.ShowError("Erro", $"Falha ao abrir revisão: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Opens a dialog to reschedule the specified event, allowing the user to select a new date and updating the
        /// event in the database if the date is changed.
        /// </summary>
        /// <remarks>If the event is successfully rescheduled, the method updates the database, refreshes
        /// the calendar view, and displays a success notification. If the event is an official date, the user is
        /// informed that it must be changed elsewhere. Any errors encountered during the update process are displayed
        /// to the user.</remarks>
        /// <param name="cronograma">The event to be rescheduled. Cannot be null. If the event represents an official date (such as a test or
        /// opening), rescheduling is not permitted.</param>
        private async void MoverEvento(EditalCronograma? cronograma)
        {
            if (cronograma == null) return;

            // 1. Validação: Bloquear edição de eventos virtuais (Datas de Prova/Abertura)
            if (cronograma.Id < 0)
            {
                _notificationService.ShowInfo(
                    "Não é possível remarcar",
                    "Esta é uma data oficial do Edital (Prova/Abertura).\n" +
                    "Para alterá-la, vá até a aba Editais e edite as informações principais do concurso.");
                return;
            }

            // 2. Configurar dados iniciais
            DateTime dataAtual = cronograma.DataEvento;

            // 3. Abrir Diálogo
            var dialog = new Views.MoverEventoDialog(dataAtual);

            // CORREÇÃO: Define o Owner buscando explicitamente a MainWindow para evitar erro de auto-referência
            var mainWindow = System.Windows.Application.Current.Windows
                                           .OfType<Window>()
                                           .FirstOrDefault(w => w.GetType().Name == "MainWindow");

            if (mainWindow != null)
            {
                dialog.Owner = mainWindow;
            }

            // Define o título usando o nome do evento
            dialog.Title = $"Remarcar {cronograma.Evento}";

            if (dialog.ShowDialog() == true)
            {
                var novaData = dialog.DataSelecionada;

                // Se a data não mudou, não faz nada
                if (novaData.Date == dataAtual.Date) return;

                try
                {
                    // 4. Buscar e Atualizar no Banco de Dados
                    var cronogramaDb = await _context.EditalCronograma.FindAsync(cronograma.Id);

                    if (cronogramaDb != null)
                    {
                        // Preserva o horário original (caso o evento tenha hora específica)
                        var horaOriginal = new DateTime(cronogramaDb.DataEventoTicks).TimeOfDay;

                        // Atualiza com a nova data + hora original
                        cronogramaDb.DataEventoTicks = (novaData.Date + horaOriginal).Ticks;

                        _context.EditalCronograma.Update(cronogramaDb);
                        await _context.SaveChangesAsync();

                        // 5. Atualizar Interface e Cache
                        DiaSelecionado = null; // Fecha o painel lateral
                        RefreshData();         // Recarrega o calendário

                        _notificationService.ShowSuccess("Sucesso", "Evento remarcado com sucesso!");
                    }
                }
                catch (Exception ex)
                {
                    MensagemErro = $"Erro ao mover evento: {ex.Message}";
                    TemErro = true;
                    _notificationService.ShowError("Erro", $"Falha ao salvar alterações: {ex.Message}");
                }
            }
        }
    }

    public class EventosDia
    {
        public DateTime Data { get; set; }
        public List<Estudo> Estudos { get; set; } = new();
        public List<EditalCronograma> EventosEditais { get; set; } = new();
        public List<Revisao> Revisoes { get; set; } = new();

        public int TotalEventos => Estudos.Count + EventosEditais.Count + Revisoes.Count;
        public bool TemEventos => TotalEventos > 0;

        public string ResumoEventos
        {
            get
            {
                var partes = new List<string>();
                if (Estudos.Count > 0) partes.Add($"{Estudos.Count} estudo(s)");
                if (EventosEditais.Count > 0) partes.Add($"{EventosEditais.Count} evento(s)");
                if (Revisoes.Count > 0) partes.Add($"{Revisoes.Count} revisão(ões)");
                return string.Join(", ", partes);
            }
        }
    }
}
