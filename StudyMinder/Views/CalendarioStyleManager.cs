using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StudyMinder.Views
{
    /// <summary>
    /// Gerenciador centralizado de estilos e cores para o calendário
    /// </summary>
    public class CalendarioStyleManager
    {
        private readonly FrameworkElement _resourceContainer;

        public CalendarioStyleManager(FrameworkElement resourceContainer)
        {
            _resourceContainer = resourceContainer;
        }

        /// <summary>
        /// Obtém um recurso de brush de forma segura
        /// </summary>
        public Brush ObterBrush(string resourceKey, Brush fallback)
        {
            try
            {
                var resource = _resourceContainer.FindResource(resourceKey);
                if (resource is Brush brush)
                {
                    return brush;
                }
            }
            catch
            {
                // Recurso não encontrado, retornar fallback
            }
            return fallback;
        }

        /// <summary>
        /// Cria um brush claro baseado em um brush original
        /// </summary>
        public Brush CriarBrushClaro(Brush originalBrush, byte opacity = 25)
        {
            if (originalBrush is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                return new SolidColorBrush(Color.FromArgb(opacity, color.R, color.G, color.B));
            }
            return originalBrush;
        }

        /// <summary>
        /// Obtém a cor de um brush
        /// </summary>
        public Color? ObterCor(Brush brush)
        {
            if (brush is SolidColorBrush solidBrush)
            {
                return solidBrush.Color;
            }
            return null;
        }

        /// <summary>
        /// Cria um efeito de sombra para dias do mês
        /// </summary>
        public System.Windows.Media.Effects.DropShadowEffect CriarEfeitoSombra(
            double opacity = 0.1,
            double blurRadius = 4,
            double shadowDepth = 2)
        {
            return new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = opacity,
                BlurRadius = blurRadius,
                ShadowDepth = shadowDepth
            };
        }

        /// <summary>
        /// Configura o estilo visual de uma borda para um dia do calendário
        /// </summary>
        public void ConfigurarEstiloDia(
            Border border,
            DateTime data,
            bool isForaMes,
            bool isHoje)
        {
            if (isHoje)
            {
                var primaryBrush = ObterBrush("PrimaryBrush", Brushes.Blue);
                border.Background = CriarBrushClaro(primaryBrush);
                border.BorderBrush = primaryBrush;
                border.BorderThickness = new Thickness(2);
            }
            else if (!isForaMes)
            {
                border.Background = ObterBrush("SurfaceBrush", Brushes.White);
                border.BorderBrush = ObterBrush("BorderBrush", Brushes.LightGray);
                border.BorderThickness = new Thickness(1);
                border.Effect = CriarEfeitoSombra();
            }
            else
            {
                border.Background = ObterBrush("SurfaceBrush", Brushes.White);
                border.BorderBrush = ObterBrush("BorderBrush", Brushes.LightGray);
                border.BorderThickness = new Thickness(1);
            }
        }

        /// <summary>
        /// Obtém a cor de texto para um dia baseado em seu estado
        /// </summary>
        public Brush ObterCorTexto(bool isForaMes, bool isHoje)
        {
            if (isForaMes)
            {
                return ObterBrush("TextSecondaryBrush", Brushes.Gray);
            }
            
            if (isHoje)
            {
                return ObterBrush("PrimaryBrush", Brushes.Blue);
            }

            return ObterBrush("TextPrimaryBrush", Brushes.Black);
        }
    }
}
