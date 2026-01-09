using StudyMinder.Utils;
using StudyMinder.ViewModels;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StudyMinder.Views
{
    public partial class ViewCalendario : UserControl, IDisposable
    {
        private CalendarioViewModel? _viewModel;
        private CalendarioRenderer? _renderer;
        private CalendarioStyleManager? _styleManager;
        private bool _disposed;

        public ViewCalendario()
        {
            InitializeComponent();
            Loaded += ViewCalendario_Loaded;
            Unloaded += ViewCalendario_Unloaded;
        }

        private async void ViewCalendario_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not CalendarioViewModel vm)
            {
                // Se o DataContext não for do tipo esperado, talvez seja melhor registrar um erro.
                // Por enquanto, apenas retornamos para evitar NullReferenceException.
                return;
            }

            _viewModel = vm;
            
            if (_styleManager == null)
                _styleManager = new CalendarioStyleManager(this);
                
            if (_renderer == null)
                _renderer = new CalendarioRenderer(CalendarioGrid, _styleManager);

            // Garante que não há inscrições duplicadas antes de inscrever
            DesinscreverEventos();

            _viewModel.RequestRender += OnRequestRender;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Associa o clique nas células do calendário ao comando do ViewModel
            CalendarioGrid.MouseLeftButtonUp += CalendarioGrid_MouseLeftButtonUp;

            // A carga inicial de dados é agora responsabilidade do ViewModel.
            // Apenas acionamos a primeira renderização.
            try
            {
                await _viewModel.CarregarDadosPeriodoAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar dados do calendário: {ex.Message}");
            }
        }

        private void ViewCalendario_Unloaded(object sender, RoutedEventArgs e)
        {
            DesinscreverEventos();
        }

        private void DesinscreverEventos()
        {
            if (_viewModel != null)
            {
                _viewModel.RequestRender -= OnRequestRender;
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            CalendarioGrid.MouseLeftButtonUp -= CalendarioGrid_MouseLeftButtonUp;
        }

        public void Dispose()
        {
            if (_disposed) return;

            DesinscreverEventos();
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_viewModel == null) return;

            switch (e.PropertyName)
            {
                case nameof(CalendarioViewModel.IsBusy):
                    Cursor = _viewModel.IsBusy ? Cursors.Wait : Cursors.Arrow;
                    break;

                case nameof(CalendarioViewModel.DataExibida):
                case nameof(CalendarioViewModel.IsVisualizacaoSemanal):
                    AtualizarPeriodoTexto();
                    break;
                
                case nameof(CalendarioViewModel.DiaSelecionado):
                    if (_viewModel.DiaSelecionado != null)
                    {
                        DiaDetalhesPanel.ExibirDetalhes(
                            _viewModel.DiaSelecionado,
                            _viewModel.MoverEventoCommand,
                            _viewModel.EditarEstudoCommand,
                            _viewModel.IniciarRevisaoCommand);
                    }
                    else
                    {
                        DiaDetalhesPanel.Visibility = Visibility.Collapsed;
                    }
                    break;
            }
        }
        
        private void OnRequestRender()
        {
            Dispatcher.Invoke(() =>
            {
                if (_viewModel == null || _renderer == null) return;

                if (_viewModel.IsVisualizacaoSemanal)
                {
                    HeaderDiasSemana.Visibility = Visibility.Collapsed;
                    _renderer.RenderizarSemanal(_viewModel.DataExibida, _viewModel.ObterCacheEventos());
                }
                else
                {
                    HeaderDiasSemana.Visibility = Visibility.Visible;
                    _renderer.RenderizarMensal(_viewModel.DataExibida, _viewModel.ObterCacheEventos());
                }
                AtualizarEstiloBotoes();
            });
        }
        
        private void CalendarioGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element && element.DataContext is DateTime data)
            {
                _viewModel?.SelecionarDiaCommand.Execute(data);
            }
        }
        
        private void AtualizarPeriodoTexto()
        {
            if (_viewModel == null) return;
            
            CultureInfo culture = new CultureInfo("pt-BR");
            
            if (_viewModel.IsVisualizacaoSemanal)
            {
                var inicioSemana = DateUtils.GetInicioSemana(_viewModel.DataExibida);
                var fimSemana = inicioSemana.AddDays(6);
                TxtPeriodo.Text = $"{inicioSemana:dd} - {fimSemana:dd} de {fimSemana.ToString("MMMM", culture)}";
            }
            else
            {
                TxtPeriodo.Text = _viewModel.DataExibida.ToString("MMMM 'de' yyyy", culture);
            }
        }
        
        private void AtualizarEstiloBotoes()
        {
            if (_viewModel == null) return;

            var primaryBrush = FindResource("PrimaryBrush") as Brush;
            var backgroundBrush = FindResource("BackgroundBrush") as Brush;
            var textPrimaryBrush = FindResource("TextPrimaryBrush") as Brush;
            var whiteBrush = Brushes.White;

            if (_viewModel.IsVisualizacaoSemanal)
            {
                BtnSemana.Background = primaryBrush;
                BtnSemana.Foreground = whiteBrush;
                BtnMes.Background = backgroundBrush;
                BtnMes.Foreground = textPrimaryBrush;
            }
            else
            {
                BtnMes.Background = primaryBrush;
                BtnMes.Foreground = whiteBrush;
                BtnSemana.Background = backgroundBrush;
                BtnSemana.Foreground = textPrimaryBrush;
            }
        }
        
        #region Event Handlers for Buttons
        private void BtnMes_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.MudarVisualizacaoCommand.Execute("Mensal");
        }

        private void BtnSemana_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.MudarVisualizacaoCommand.Execute("Semanal");
        }



        private void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.NavegarPeriodoCommand.Execute("Anterior");
        }

        private void BtnProximo_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.NavegarPeriodoCommand.Execute("Proximo");
        }

        private void BtnHoje_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.IrParaHojeCommand.Execute(null);
        }
        #endregion
    }
}
