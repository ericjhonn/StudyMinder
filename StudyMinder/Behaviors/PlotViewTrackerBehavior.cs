using Microsoft.Xaml.Behaviors;
using OxyPlot;
using OxyPlot.Wpf;
using System.Windows;
using System.Windows.Controls;
using StudyMinder.Models;
using StudyMinder.ViewModels;

namespace StudyMinder.Behaviors
{
    /// <summary>
    /// Behavior para exibir tooltips com dados dos editais no PlotView de RendimentoProva
    /// </summary>
    public class PlotViewTrackerBehavior : Behavior<PlotView>
    {
        public static readonly DependencyProperty DadosEditaisProperty =
            DependencyProperty.Register(
                nameof(DadosEditais),
                typeof(List<Edital>),
                typeof(PlotViewTrackerBehavior),
                new PropertyMetadata(null, OnDadosEditaisChanged));

        public List<Edital>? DadosEditais
        {
            get => (List<Edital>?)GetValue(DadosEditaisProperty);
            set => SetValue(DadosEditaisProperty, value);
        }

        private static void OnDadosEditaisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG PlotViewTrackerBehavior] DadosEditais alterado: {(e.NewValue as List<Edital>)?.Count ?? 0} itens");
        }

        private ToolTip? _tooltip;
        private int _ultimoPontoExibido = -1;  // Rastreia qual ponto estava sendo exibido

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject != null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG PlotViewTrackerBehavior] Behavior anexado");
                AssociatedObject.MouseMove += OnPlotViewMouseMove;
                AssociatedObject.MouseLeave += OnPlotViewMouseLeave;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject != null)
            {
                AssociatedObject.MouseMove -= OnPlotViewMouseMove;
                AssociatedObject.MouseLeave -= OnPlotViewMouseLeave;
            }
        }

        private void OnPlotViewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (AssociatedObject?.ActualModel == null)
                    return;

                if (DadosEditais == null || DadosEditais.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] DadosEditais vazio");
                    return;
                }

                var point = e.GetPosition(AssociatedObject);
                var plotArea = AssociatedObject.ActualModel.PlotArea;

                // Verificar se está dentro da área do gráfico
                if (point.X < plotArea.Left || point.X > plotArea.Right || 
                    point.Y < plotArea.Top || point.Y > plotArea.Bottom)
                {
                    // Saiu da área do gráfico - fechar tooltip
                    if (_tooltip?.IsOpen == true)
                    {
                        _tooltip.IsOpen = false;
                        _ultimoPontoExibido = -1;
                        System.Diagnostics.Debug.WriteLine("[DEBUG] Saiu da área do gráfico - tooltip fechado");
                    }
                    return;
                }

                // Converter ponto da tela para coordenadas do gráfico
                try
                {
                    double dataX = AssociatedObject.ActualModel.Axes[0].InverseTransform(point.X);
                    int indiceProximo = (int)Math.Round(dataX);

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] dataX={dataX:F2}, índice={indiceProximo}");

                    // Verificar se está dentro do intervalo válido
                    if (indiceProximo >= 0 && indiceProximo < DadosEditais.Count)
                    {
                        // Verificar se mudou de ponto
                        if (indiceProximo != _ultimoPontoExibido)
                        {
                            // Ponto mudou - fechar tooltip anterior
                            if (_tooltip?.IsOpen == true)
                            {
                                _tooltip.IsOpen = false;
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] Mudou do ponto {_ultimoPontoExibido} para {indiceProximo} - tooltip fechado");
                            }

                            // Atualizar ponto ativo
                            _ultimoPontoExibido = indiceProximo;

                            var edital = DadosEditais[indiceProximo];
                            var acertos = edital.AcertosProva ?? 0;
                            var erros = edital.ErrosProva ?? 0;
                            var total = acertos + erros;
                            var rendimento = edital.RendimentoProva ?? 0m;
                            var dataProva = new DateTime(edital.DataProvaTicks);

                            // Montar conteúdo do tooltip
                            string titulo = $"{indiceProximo + 1}º concurso";
                            string data = dataProva.ToString("dd/MM/yyyy");
                            string banca = edital.Banca ?? "N/A";
                            string orgao = edital.Orgao ?? "N/A";
                            string cargo = edital.Cargo ?? "N/A";
                            string area = edital.Area ?? "N/A";
                            string perc = $"{rendimento:F2}%";

                            string conteudo = $"{titulo}\n\n{data} - {banca} - {orgao}\n{cargo}\nÁrea: {area}\n\n{perc} de aproveitamento";

                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Ponto {indiceProximo}: {orgao} | A={acertos} E={erros} Total={total} | RendimentoProva={rendimento:F2}%");

                            // Criar tooltip se não existir
                            if (_tooltip == null)
                            {
                                _tooltip = new ToolTip
                                {
                                    Content = conteudo,
                                    Padding = new Thickness(12),
                                    FontSize = 12,
                                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                                    Background = System.Windows.Media.Brushes.White,
                                    Foreground = System.Windows.Media.Brushes.Black,
                                    BorderThickness = new Thickness(1),
                                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                                    IsOpen = false
                                };
                                AssociatedObject.ToolTip = _tooltip;
                            }
                            else
                            {
                                _tooltip.Content = conteudo;
                            }

                            // Abrir tooltip para novo ponto
                            _tooltip.IsOpen = true;
                        }
                        else
                        {
                            // Mesmo ponto - manter tooltip aberto
                            if (_tooltip?.IsOpen == false)
                            {
                                _tooltip.IsOpen = true;
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] Mantém ponto {indiceProximo} - tooltip reaberto");
                            }
                        }
                    }
                    else
                    {
                        // Fora do intervalo de pontos válidos
                        if (_tooltip?.IsOpen == true)
                        {
                            _tooltip.IsOpen = false;
                            _ultimoPontoExibido = -1;
                            System.Diagnostics.Debug.WriteLine("[DEBUG] Fora do intervalo - tooltip fechado");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Erro ao calcular dataX]: {ex.Message}");
                    if (_tooltip?.IsOpen == true)
                        _tooltip.IsOpen = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Erro PlotViewTrackerBehavior.OnPlotViewMouseMove]: {ex.Message}");
            }
        }

        private void OnPlotViewMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_tooltip?.IsOpen == true)
            {
                _tooltip.IsOpen = false;
                _ultimoPontoExibido = -1;
                System.Diagnostics.Debug.WriteLine("[DEBUG] Mouse deixou a área - tooltip fechado");
            }
        }
    }
}
