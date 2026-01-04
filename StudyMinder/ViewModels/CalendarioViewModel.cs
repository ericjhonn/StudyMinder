using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using StudyMinder.Data;
using StudyMinder.Models;
using StudyMinder.Services;
using StudyMinder.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StudyMinder.ViewModels
{
    public partial class CalendarioViewModel : BaseViewModel, IRefreshable
    {
        private readonly StudyMinderContext _context;

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
            EditalCronogramaNotificacaoService editalCronogramaNotificacaoService)
        {
            _context = context;
            Title = "Calendário de Estudos";
            _dataExibida = DateTime.Today;

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

            var estudosTask = _context.Estudos.AsNoTracking().Include(e => e.Assunto)
                                      .Where(e => e.DataTicks >= deTicks && e.DataTicks <= ateTicks)
                                      .ToListAsync(token);

            var eventosTask = _context.EditalCronograma.AsNoTracking().Include(ec => ec.Edital)
                                      .Where(e => e.DataEventoTicks >= de.Ticks && e.DataEventoTicks <= ate.Ticks)
                                      .ToListAsync(token);

            var revisoesTask = _context.Revisoes.AsNoTracking().Include(r => r.EstudoOrigem.Assunto)
                                       .Where(r => r.DataProgramadaTicks >= de.Ticks && r.DataProgramadaTicks <= ate.Ticks)
                                       .ToListAsync(token);

            await Task.WhenAll(estudosTask, eventosTask, revisoesTask);
            return (await estudosTask, await eventosTask, await revisoesTask);
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
