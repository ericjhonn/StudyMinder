using System.Windows.Media;

namespace StudyMinder.Models
{
    /// <summary>
    /// Mapeia cores para tipos de estudo de forma consistente
    /// </summary>
    public static class TipoEstudoColorMap
    {
        private static readonly Dictionary<string, Color> ColorMap = new();
        private static readonly List<Color> DefaultColors = new()
        {
            Color.FromRgb(0x4C, 0xC9, 0x71),  // Verde - Success
            Color.FromRgb(0x21, 0x96, 0xF3),  // Azul - Info
            Color.FromRgb(0xFF, 0xB9, 0x00),  // Amarelo - Warning
            Color.FromRgb(0xE0, 0x6C, 0x00),  // Laranja - Horas
            Color.FromRgb(0xB1, 0x46, 0xC2)   // Roxo - Rendimento
        };

        static TipoEstudoColorMap()
        {
            // Inicializar com cores padrão
            InitializeDefaultColors();
        }

        private static void InitializeDefaultColors()
        {
            ColorMap.Clear();
            var colors = DefaultColors;
            int colorIndex = 0;

            // Mapear tipos comuns de estudo
            var tiposComuns = new[] { "Questões", "Leitura", "Resumo", "Exercício", "Revisão" };
            foreach (var tipo in tiposComuns)
            {
                if (colorIndex < colors.Count)
                {
                    ColorMap[tipo] = colors[colorIndex++];
                }
            }
        }

        /// <summary>
        /// Obtém a cor para um tipo de estudo específico
        /// </summary>
        public static Color GetColor(string tipoEstudoNome)
        {
            if (string.IsNullOrEmpty(tipoEstudoNome))
                return DefaultColors[0];

            if (ColorMap.TryGetValue(tipoEstudoNome, out var color))
                return color;

            // Se não encontrar, atribuir uma cor baseada no hash do nome
            int hash = tipoEstudoNome.GetHashCode();
            int colorIndex = Math.Abs(hash) % DefaultColors.Count;
            var assignedColor = DefaultColors[colorIndex];

            ColorMap[tipoEstudoNome] = assignedColor;
            return assignedColor;
        }

        /// <summary>
        /// Obtém um brush para um tipo de estudo
        /// </summary>
        public static SolidColorBrush GetBrush(string tipoEstudoNome)
        {
            var color = GetColor(tipoEstudoNome);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// Obtém lista de brushes para todos os tipos de estudo
        /// </summary>
        public static List<SolidColorBrush> GetBrushes(IEnumerable<string> tiposEstudo)
        {
            return tiposEstudo
                .Select(GetBrush)
                .ToList();
        }
    }
}
