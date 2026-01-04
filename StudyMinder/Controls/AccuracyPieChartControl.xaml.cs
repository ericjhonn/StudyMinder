using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Linq;
using OxyPlot;
using OxyPlot.Series;
using System.Collections.ObjectModel;
using StudyMinder.Models;
using OxyPlot.Legends;

namespace StudyMinder.Controls
{
    public partial class AccuracyPieChartControl : UserControl, INotifyPropertyChanged
    {
        // --- NOVO: Dados Genéricos (reutilizável em Home e Comparador) ---
        public static readonly DependencyProperty DadosProperty =
            DependencyProperty.Register("Dados", typeof(ObservableCollection<GraficoChartData>), typeof(AccuracyPieChartControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDadosChanged));

        // --- LEGADO: Propriedades específicas (backward compatibility com Home) ---
        public static readonly DependencyProperty AcertosProperty =
            DependencyProperty.Register("Acertos", typeof(int), typeof(AccuracyPieChartControl),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDataChanged));

        public static readonly DependencyProperty ErrosProperty =
            DependencyProperty.Register("Erros", typeof(int), typeof(AccuracyPieChartControl),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDataChanged));

        // --- NOVO: Dados Genéricos (Comparador, etc.) ---
        public ObservableCollection<GraficoChartData> Dados
        {
            get { return (ObservableCollection<GraficoChartData>)GetValue(DadosProperty); }
            set { SetValue(DadosProperty, value); }
        }

        // --- LEGADO: Propriedades específicas (Home com Acertos/Erros) ---
        public int Acertos
        {
            get { return (int)GetValue(AcertosProperty); }
            set { SetValue(AcertosProperty, value); }
        }

        public int Erros
        {
            get { return (int)GetValue(ErrosProperty); }
            set { SetValue(ErrosProperty, value); }
        }

        public static readonly DependencyProperty PlotModelAccuracyPieProperty =
            DependencyProperty.Register("PlotModelAccuracyPie", typeof(PlotModel), typeof(AccuracyPieChartControl),
                new PropertyMetadata(null));

        public PlotModel PlotModelAccuracyPie
        {
            get => (PlotModel)GetValue(PlotModelAccuracyPieProperty);
            set => SetValue(PlotModelAccuracyPieProperty, value);
        }

        public static readonly DependencyProperty PlotControllerAccuracyPieProperty =
            DependencyProperty.Register("PlotControllerAccuracyPie", typeof(PlotController), typeof(AccuracyPieChartControl),
                new PropertyMetadata(null));

        public PlotController PlotControllerAccuracyPie
        {
            get => (PlotController)GetValue(PlotControllerAccuracyPieProperty);
            set => SetValue(PlotControllerAccuracyPieProperty, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public AccuracyPieChartControl()
        {
            InitializeComponent();
            
            // Inicializar o PlotController
            SetValue(PlotControllerAccuracyPieProperty, new PlotController());
            
            // NÃO definir DataContext aqui - deixar o binding do pai funcionar
            // DataContext será herdado de ViewHome (HomeViewModel)
            
            // Inicializar o gráfico vazio
            ConfigurarGraficoPizza();
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] AccuracyPieChartControl construtor: PlotControllerAccuracyPie={PlotControllerAccuracyPie}");
            
            // Quando o controle é carregado, forçar atualização dos valores
            this.Loaded += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] AccuracyPieChartControl.Loaded: Acertos={Acertos}, Erros={Erros}, DataContext={DataContext?.GetType().Name}");
                ConfigurarGraficoPizza();
            };
        }

        private static void OnDadosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AccuracyPieChartControl)d;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] OnDadosChanged - Dados property changed");
            control.ConfigurarGraficoPizza();
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AccuracyPieChartControl)d;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] OnDataChanged - Property: {e.Property.Name}, Old: {e.OldValue}, New: {e.NewValue}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] OnDataChanged: Acertos={control.Acertos}, Erros={control.Erros}");
            control.ConfigurarGraficoPizza();
        }

        private void ConfigurarGraficoPizza()
        {
            try
            {
                Legend legend = new Legend
                {
                    LegendPosition = LegendPosition.BottomCenter,
                    LegendOrientation = LegendOrientation.Horizontal,
                    LegendPlacement = LegendPlacement.Outside,
                    FontSize = 11,
                    TextColor = OxyColors.Gray
                };

                // --- 1. Modelo com Legenda ---
                var plotModel = new PlotModel
                {
                    Background = OxyColors.Transparent,
                    PlotAreaBorderThickness = new OxyThickness(0),
                    PlotMargins = new OxyThickness(0),
                    Title = string.Empty,
                    TextColor = OxyColors.Gray,
                };

                plotModel.Legends.Clear();
                plotModel.Legends.Add(legend);
                
                // --- 2. Série Pizza (Estilo Donut) ---
                var pieSeries = new PieSeries
                {
                    InnerDiameter = 0.60,
                    Stroke = OxyColors.White,
                    StrokeThickness = 3.0,
                    InsideLabelPosition = 0.65,
                    InsideLabelFormat = "{0}\n{1:N0}\n({2:P1})",  // Nome, Valor, Percentual
                    InsideLabelColor = OxyColors.White,
                    OutsideLabelFormat = null,
                    TickHorizontalLength = 0,
                    TickRadialLength = 0,
                    FontSize = 11,
                    FontWeight = OxyPlot.FontWeights.Normal,
                    TrackerFormatString = "{0}: {1:N0} ({2:P1})"
                };

                // --- MODO 1: Dados Genéricos (Comparador com GraficoChartData) ---
                if (Dados != null && Dados.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ConfigurarGraficoPizza - Modo Genérico: {Dados.Count} itens");
                    
                    // Calcular o total para os percentuais
                    int total = Dados.Sum(d => d.Value);
                    
                    foreach (var item in Dados)
                    {
                        var cor = OxyColor.Parse(item.Color);
                        
                        var slice = new PieSlice(item.Title, item.Value)
                        {
                            Fill = cor,
                            IsExploded = false
                        };
                        pieSeries.Slices.Add(slice);
                    }
                }
                // --- MODO 2: Legado (Home com Acertos/Erros) ---
                else
                {
                    int total = Acertos + Erros;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ConfigurarGraficoPizza - Modo Legado: Acertos={Acertos}, Erros={Erros}");

                    pieSeries.TrackerFormatString = "{0}\n{1:N0} questões ({2:P1})";

                    if (total > 0)
                    {
                        var sliceAcertos = new PieSlice("Acertos", Acertos)
                        {
                            Fill = OxyColor.FromRgb(102, 187, 106),
                            IsExploded = true
                        };
                        pieSeries.Slices.Add(sliceAcertos);

                        var sliceErros = new PieSlice("Erros", Erros)
                        {
                            Fill = OxyColor.FromRgb(239, 83, 80),
                            IsExploded = true
                        };
                        pieSeries.Slices.Add(sliceErros);
                    }
                    else
                    {
                        pieSeries.Slices.Add(new PieSlice("", 1)
                        {
                            Fill = OxyColor.FromRgb(230, 230, 230),
                            IsExploded = false
                        });

                        pieSeries.InsideLabelFormat = "";
                        pieSeries.TrackerFormatString = "Sem dados registrados";
                    }
                }

                plotModel.Series.Add(pieSeries);
                PlotModelAccuracyPie = plotModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Erro Pizza]: {ex.Message}");
            }
        }

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
