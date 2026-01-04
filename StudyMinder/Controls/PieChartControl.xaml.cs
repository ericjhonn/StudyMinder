using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using StudyMinder.Models;

namespace StudyMinder.Controls
{
    public partial class PieChartControl : UserControl
    {
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register(
                nameof(DataSource),
                typeof(ObservableCollection<PizzaChartData>),
                typeof(PieChartControl),
                new PropertyMetadata(null, OnDataSourceChanged));

        public static readonly DependencyProperty ColorsProperty =
            DependencyProperty.Register(
                nameof(Colors),
                typeof(ObservableCollection<SolidColorBrush>),
                typeof(PieChartControl),
                new PropertyMetadata(null, OnColorsChanged));

        public ObservableCollection<PizzaChartData> DataSource
        {
            get => (ObservableCollection<PizzaChartData>)GetValue(DataSourceProperty);
            set => SetValue(DataSourceProperty, value);
        }

        public ObservableCollection<SolidColorBrush> Colors
        {
            get => (ObservableCollection<SolidColorBrush>)GetValue(ColorsProperty);
            set => SetValue(ColorsProperty, value);
        }

        public PieChartControl()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] PieChartControl carregado");
                DrawPieChart();
            };
        }

        private static void OnDataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PieChartControl control)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] DataSource alterado");
                
                // Desinscrever do evento anterior
                if (e.OldValue is ObservableCollection<PizzaChartData> oldCollection)
                {
                    oldCollection.CollectionChanged -= control.OnDataSourceCollectionChanged;
                }

                // Inscrever no novo evento
                if (e.NewValue is ObservableCollection<PizzaChartData> newCollection)
                {
                    newCollection.CollectionChanged += control.OnDataSourceCollectionChanged;
                    
                    // Desenhar se já temos dados
                    if (newCollection.Count > 0 && control.Colors != null && control.Colors.Count > 0)
                    {
                        control.DrawPieChart();
                    }
                }
            }
        }

        private void OnDataSourceCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Coleção DataSource alterada. Ação: {e.Action}, Count: {DataSource?.Count ?? 0}");
            
            // Só desenhar se temos dados E cores
            if (DataSource != null && DataSource.Count > 0 && Colors != null && Colors.Count > 0)
            {
                DrawPieChart();
            }
        }

        private static void OnColorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PieChartControl control)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Colors alterado");

                // Desinscrever do evento anterior
                if (e.OldValue is ObservableCollection<SolidColorBrush> oldCollection)
                {
                    oldCollection.CollectionChanged -= control.OnColorsCollectionChanged;
                }

                // Inscrever no novo evento
                if (e.NewValue is ObservableCollection<SolidColorBrush> newCollection)
                {
                    newCollection.CollectionChanged += control.OnColorsCollectionChanged;
                    
                    // Desenhar se já temos dados
                    if (newCollection.Count > 0 && control.DataSource != null && control.DataSource.Count > 0)
                    {
                        control.DrawPieChart();
                    }
                }
            }
        }

        private void OnColorsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Coleção Colors alterada. Ação: {e.Action}, Count: {Colors?.Count ?? 0}");
            
            // Só desenhar se temos dados E cores
            if (DataSource != null && DataSource.Count > 0 && Colors != null && Colors.Count > 0)
            {
                DrawPieChart();
            }
        }

        private void DrawPieChart()
        {
            try
            {
                if (PieCanvas == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] PieCanvas é null!");
                    return;
                }

                PieCanvas.Children.Clear();

                if (DataSource == null || DataSource.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] DataSource vazio ou null. DataSource={DataSource}, Count={DataSource?.Count ?? 0}");
                    return;
                }

                if (Colors == null || Colors.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Colors vazio ou null. Colors={Colors}, Count={Colors?.Count ?? 0}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Desenhando pizza com {DataSource.Count} itens e {Colors.Count} cores");

                double centerX = 90;
                double centerY = 90;
                double radius = 70;

                double startAngle = -90;

                for (int i = 0; i < DataSource.Count; i++)
                {
                    var item = DataSource[i];
                    double sweepAngle = (item.Percentual / 100.0) * 360;

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Fatia {i}: {item.TipoEstudoNome} - {item.Percentual:F1}% - Ângulo: {sweepAngle:F1}°");

                    // Desenhar fatia
                    var path = CreatePieSlice(centerX, centerY, radius, startAngle, sweepAngle, Colors[i % Colors.Count]);
                    PieCanvas.Children.Add(path);

                    // Desenhar label com percentual
                    DrawLabel(centerX, centerY, radius, startAngle, sweepAngle, item.Percentual);

                    startAngle += sweepAngle;
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Pizza desenhada com sucesso! Total de elementos: {PieCanvas.Children.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro ao desenhar pizza: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
            }
        }

        private Path CreatePieSlice(double centerX, double centerY, double radius, double startAngle, double sweepAngle, SolidColorBrush color)
        {
            try
            {
                PathGeometry pathGeometry = new PathGeometry();
                
                // Caso especial: círculo completo (100%)
                if (Math.Abs(sweepAngle - 360) < 0.01)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Desenhando círculo completo (100%)");
                    
                    // Desenhar um círculo usando dois arcos de 180°
                    double startRad = startAngle * Math.PI / 180;
                    double x1 = centerX + radius * Math.Cos(startRad);
                    double y1 = centerY + radius * Math.Sin(startRad);
                    
                    double x2 = centerX + radius * Math.Cos(startRad + Math.PI);
                    double y2 = centerY + radius * Math.Sin(startRad + Math.PI);
                    
                    double x3 = centerX + radius * Math.Cos(startRad + 2 * Math.PI);
                    double y3 = centerY + radius * Math.Sin(startRad + 2 * Math.PI);

                    var pathFigure = new PathFigure
                    {
                        StartPoint = new Point(x1, y1),
                        IsClosed = true
                    };

                    // Primeiro arco de 180°
                    pathFigure.Segments.Add(new ArcSegment
                    {
                        Point = new Point(x2, y2),
                        Size = new Size(radius, radius),
                        IsLargeArc = false,
                        SweepDirection = SweepDirection.Clockwise
                    });

                    // Segundo arco de 180°
                    pathFigure.Segments.Add(new ArcSegment
                    {
                        Point = new Point(x3, y3),
                        Size = new Size(radius, radius),
                        IsLargeArc = false,
                        SweepDirection = SweepDirection.Clockwise
                    });

                    pathGeometry.Figures.Add(pathFigure);
                }
                else
                {
                    // Caso normal: fatia de pizza
                    double startRad = startAngle * Math.PI / 180;
                    double sweepRad = sweepAngle * Math.PI / 180;

                    double x1 = centerX + radius * Math.Cos(startRad);
                    double y1 = centerY + radius * Math.Sin(startRad);

                    double x2 = centerX + radius * Math.Cos(startRad + sweepRad);
                    double y2 = centerY + radius * Math.Sin(startRad + sweepRad);

                    bool isLargeArc = sweepAngle > 180;

                    var pathFigure = new PathFigure
                    {
                        StartPoint = new Point(centerX, centerY),
                        IsClosed = true
                    };

                    pathFigure.Segments.Add(new LineSegment { Point = new Point(x1, y1) });
                    pathFigure.Segments.Add(new ArcSegment
                    {
                        Point = new Point(x2, y2),
                        Size = new Size(radius, radius),
                        IsLargeArc = isLargeArc,
                        SweepDirection = SweepDirection.Clockwise
                    });

                    pathGeometry.Figures.Add(pathFigure);
                }

                SolidColorBrush strokeBrush = null;
                try
                {
                    strokeBrush = (SolidColorBrush)Application.Current.Resources["CardBrush"];
                }
                catch
                {
                    strokeBrush = new SolidColorBrush(System.Windows.Media.Colors.Gray);
                }

                var path = new Path
                {
                    Data = pathGeometry,
                    Fill = color,
                    Stroke = strokeBrush,
                    StrokeThickness = 2
                };

                return path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Erro ao criar fatia: {ex.Message}");
                throw;
            }
        }

        private void DrawLabel(double centerX, double centerY, double radius, double startAngle, double sweepAngle, double percentual)
        {
            // Posicionar label no meio da fatia
            double labelAngle = startAngle + sweepAngle / 2;
            double labelRad = labelAngle * Math.PI / 180;
            double labelRadius = radius * 0.65;

            double labelX = centerX + labelRadius * Math.Cos(labelRad);
            double labelY = centerY + labelRadius * Math.Sin(labelRad);

            // Mostrar percentual apenas se > 5%
            if (percentual >= 5)
            {
                var textBlock = new TextBlock
                {
                    Text = $"{percentual:F0}%",
                    Foreground = (SolidColorBrush)Application.Current.Resources["TextPrimaryBrush"],
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                Canvas.SetLeft(textBlock, labelX - 15);
                Canvas.SetTop(textBlock, labelY - 10);

                PieCanvas.Children.Add(textBlock);
            }
        }
    }
}
