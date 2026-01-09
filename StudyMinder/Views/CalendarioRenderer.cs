using StudyMinder.Utils;
using StudyMinder.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StudyMinder.Views
{
    /// <summary>
    /// Renderizador centralizado para elementos do calendário
    /// Separa a lógica de criação de UI da lógica de negócio
    /// </summary>
    public class CalendarioRenderer
    {
        private readonly CalendarioStyleManager _styleManager;
        private readonly Grid _grid;
        private const int DIAS_SEMANA = 7;
        private const int HORA_INICIO = 0;
        private const int HORA_FIM = 23;
        private const int COLUNA_HORARIO_WIDTH = 80;
        private const int ALTURA_LINHA_HORA = 60;
        private const int ALTURA_HEADER = 40;

        public CalendarioRenderer(Grid grid, CalendarioStyleManager styleManager)
        {
            _grid = grid;
            _styleManager = styleManager;
        }

        /// <summary>
        /// Renderiza um calendário mensal
        /// </summary>
        public void RenderizarMensal(DateTime dataAtual, System.Collections.Generic.Dictionary<DateTime, EventosDia> cache)
        {
            LimparGrid();

            // Configurar colunas
            for (int i = 0; i < DIAS_SEMANA; i++)
            {
                _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            var primeiroDia = new DateTime(dataAtual.Year, dataAtual.Month, 1);
            var ultimoDia = primeiroDia.AddMonths(1).AddDays(-1);
            var inicioCalendario = DateUtils.GetInicioSemana(primeiroDia);

            var totalSemanas = (int)Math.Ceiling((ultimoDia - inicioCalendario).TotalDays / 7.0);
             if (inicioCalendario.AddDays(totalSemanas * 7).CompareTo(ultimoDia) < 0)
                totalSemanas++;


            // Criar linhas
            for (int i = 0; i < totalSemanas; i++)
            {
                _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            // Preencher dias
            for (int semana = 0; semana < totalSemanas; semana++)
            {
                for (int dia = 0; dia < DIAS_SEMANA; dia++)
                {
                    var data = inicioCalendario.AddDays(semana * DIAS_SEMANA + dia);
                    var eventos = cache.ContainsKey(data.Date) ? cache[data.Date] : new EventosDia { Data = data.Date };
                    var border = CriarDiaCalendario(data, eventos, dataAtual.Month != data.Month);
                    Grid.SetRow(border, semana);
                    Grid.SetColumn(border, dia);
                    _grid.Children.Add(border);
                }
            }
        }

        /// <summary>
        /// Renderiza um calendário semanal
        /// </summary>
        public void RenderizarSemanal(DateTime dataAtual, System.Collections.Generic.Dictionary<DateTime, EventosDia> cache)
        {
            LimparGrid();

            // Configurar colunas
            _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(COLUNA_HORARIO_WIDTH) });
            for (int i = 0; i < DIAS_SEMANA; i++)
            {
                _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            var inicioSemana = DateUtils.GetInicioSemana(dataAtual);

            // Cabeçalho dos dias
            for (int i = 0; i < DIAS_SEMANA; i++)
            {
                var dia = inicioSemana.AddDays(i);
                var header = new TextBlock
                {
                    Text = $"{dia:dd}\n{dia:ddd}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = dia.Date == DateTime.Today ?
                        _styleManager.ObterBrush("PrimaryBrush", Brushes.Blue) :
                        _styleManager.ObterBrush("TextPrimaryBrush", Brushes.Black)
                };
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, i + 1);
                _grid.Children.Add(header);
            }

            // Linhas para cada hora
            _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ALTURA_HEADER) });

            for (int hora = HORA_INICIO; hora <= HORA_FIM; hora++)
            {
                _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ALTURA_LINHA_HORA) });

                // Label da hora
                var horaLabel = new TextBlock
                {
                    Text = $"{hora:00}:00",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Foreground = _styleManager.ObterBrush("TextSecondaryBrush", Brushes.Gray),
                    Margin = new Thickness(0, 5, 10, 0)
                };
                Grid.SetRow(horaLabel, hora - HORA_INICIO + 1);
                Grid.SetColumn(horaLabel, 0);
                _grid.Children.Add(horaLabel);

                // Células para cada dia
                for (int dia = 0; dia < DIAS_SEMANA; dia++)
                {
                    var data = inicioSemana.AddDays(dia).Date.AddHours(hora);
                    var eventos = cache.ContainsKey(data.Date) ? cache[data.Date] : new EventosDia { Data = data.Date };
                    var border = CriarCelulaHora(data, eventos);
                    Grid.SetRow(border, hora - HORA_INICIO + 1);
                    Grid.SetColumn(border, dia + 1);
                    _grid.Children.Add(border);
                }
            }
        }

        /// <summary>
        /// Cria um Border para um dia do calendário
        /// </summary>
        private Border CriarDiaCalendario(DateTime data, EventosDia eventos, bool isForaMes)
        {
            var border = new Border
            {
                Background = _styleManager.ObterBrush("SurfaceBrush", Brushes.White),
                BorderBrush = _styleManager.ObterBrush("BorderBrush", Brushes.LightGray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(4),
                Padding = new Thickness(8),
                Cursor = Cursors.Hand,
                DataContext = data // Atribui a data ao DataContext para o evento de clique
            };

            _styleManager.ConfigurarEstiloDia(border, data, isForaMes, data.Date == DateTime.Today);

            var stackPanel = new StackPanel();

            // Número do dia
            var diaText = new TextBlock
            {
                Text = data.Day.ToString(),
                FontWeight = data.Date == DateTime.Today ? FontWeights.Bold : FontWeights.Normal,
                Foreground = _styleManager.ObterCorTexto(isForaMes, data.Date == DateTime.Today),
                FontSize = 14
            };
            stackPanel.Children.Add(diaText);

            // Eventos
            foreach (var estudo in eventos.Estudos.Take(2)) // Limitar para não sobrecarregar a UI
            {
                var estudoText = new TextBlock
                {
                    Text = $"{estudo.Assunto?.Nome}",
                    FontSize = 12,
                    Foreground = _styleManager.ObterBrush("PrimaryBrush", Brushes.Blue),
                    Margin = new Thickness(0, 2, 0, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                stackPanel.Children.Add(estudoText);
            }

            foreach (var evento in eventos.EventosEditais.Take(2))
            {
                var eventoText = new TextBlock
                {
                    Text = $"{evento.Evento}",
                    FontSize = 12,
                    Foreground = _styleManager.ObterBrush("SuccessBrush", Brushes.Green),
                    Margin = new Thickness(0, 2, 0, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                stackPanel.Children.Add(eventoText);
            }

            foreach (var revisao in eventos.Revisoes.Take(2))
            {
                var revisaoText = new TextBlock
                {
                    Text = $"{revisao.EstudoOrigem?.Assunto?.Nome}",
                    FontSize = 12,
                    Foreground = _styleManager.ObterBrush("WarningBrush", Brushes.Orange),
                    Margin = new Thickness(0, 2, 0, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                stackPanel.Children.Add(revisaoText);
            }

            border.Child = stackPanel;
            return border;
        }

        /// <summary>
        /// Cria um Border para uma célula de hora
        /// </summary>
        private Border CriarCelulaHora(DateTime data, EventosDia eventos)
        {
            var border = new Border
            {
                Background = _styleManager.ObterBrush("SurfaceBrush", Brushes.White),
                BorderBrush = _styleManager.ObterBrush("BorderBrush", Brushes.LightGray),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4),
                Cursor = Cursors.Hand,
                DataContext = data // Atribui a data ao DataContext para o evento de clique
            };

            var stackPanel = new StackPanel();

            // Eventos neste horário
            var eventosEstudo = eventos.Estudos.Where(e => e.Data.Hour == data.Hour).ToList();
            var eventosEditais = eventos.EventosEditais.Where(e => e.DataEvento.Hour == data.Hour).ToList();
            var revisoes = eventos.Revisoes.Where(r => r.DataProgramada.Hour == data.Hour).ToList();

            if (eventosEstudo.Count > 0 || eventosEditais.Count > 0 || revisoes.Count > 0)
            {
                foreach (var estudo in eventosEstudo)
                {
                    var estudoText = new TextBlock
                    {
                        Text = $"{estudo.Assunto?.Nome}",
                        FontSize = 12,
                        Foreground = _styleManager.ObterBrush("PrimaryBrush", Brushes.Blue),
                        Margin = new Thickness(0, 2, 0, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    stackPanel.Children.Add(estudoText);
                }

                foreach (var evento in eventosEditais)
                {
                    var eventoText = new TextBlock
                    {
                        Text = $"{evento.Evento}",
                        FontSize = 12,
                        Foreground = _styleManager.ObterBrush("SuccessBrush", Brushes.Green),
                        Margin = new Thickness(0, 2, 0, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    stackPanel.Children.Add(eventoText);
                }

                foreach (var revisao in revisoes)
                {
                    var revisaoText = new TextBlock
                    {
                        Text = $"{revisao.EstudoOrigem?.Assunto?.Nome}",
                        FontSize = 12,
                        Foreground = _styleManager.ObterBrush("WarningBrush", Brushes.Orange),
                        Margin = new Thickness(0, 2, 0, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    stackPanel.Children.Add(revisaoText);
                }

                border.Child = stackPanel;
            }

            return border;
        }

        private void LimparGrid()
        {
            _grid.Children.Clear();
            _grid.RowDefinitions.Clear();
            _grid.ColumnDefinitions.Clear();
        }
    }
}
