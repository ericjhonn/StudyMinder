namespace StudyMinder.Models
{
    public class PizzaChartData
    {
        public string TipoEstudoNome { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public double Percentual { get; set; }

        public PizzaChartData(string tipoEstudoNome, int quantidade, int total)
        {
            TipoEstudoNome = tipoEstudoNome;
            Quantidade = quantidade;
            Percentual = total > 0 ? (quantidade * 100.0 / total) : 0;
        }
    }
}
