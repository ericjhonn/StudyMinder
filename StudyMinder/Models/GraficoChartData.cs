namespace StudyMinder.Models
{
    /// <summary>
    /// Classe genérica para dados de gráficos (Pie Chart, Donut, etc.)
    /// Reutilizável em múltiplos contextos (Home, Comparador, etc.)
    /// </summary>
    public class GraficoChartData
    {
        /// <summary>
        /// Título/Label da fatia do gráfico
        /// Ex: "Aproveitável", "Garantido", "Alta Prioridade"
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Valor numérico (quantidade, contagem, etc.)
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Cor em formato hex (ex: "#4CAF50")
        /// </summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// Construtor padrão
        /// </summary>
        public GraficoChartData() { }

        /// <summary>
        /// Construtor com inicialização
        /// </summary>
        public GraficoChartData(string title, int value, string color)
        {
            Title = title;
            Value = value;
            Color = color;
        }
    }
}
