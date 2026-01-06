using System.ComponentModel;

namespace StudyMinder.Models
{
    // Modelo para exibir assuntos com menor rendimento
    public class AssuntoRendimento
    {
        public string Nome { get; set; } = string.Empty;
        public Disciplina? Disciplina { get; set; }
        public double RendimentoPercentual { get; set; }
        public int TotalAcertos { get; set; }
        public int TotalErros { get; set; }
        public int TotalQuestoes { get; set; }
        public double HorasEstudadas { get; set; }
        public bool Concluido { get; set; }
        
        // Aliases para compatibilidade com XAML
        public double Rendimento => RendimentoPercentual;
    }

    // Modelo para exibir próximas revisões
    public class RevisaoProxima
    {
        public DateTime DataRevisao { get; set; }
        public Assunto Assunto { get; set; } = null!;
        public TipoRevisaoEnum TipoRevisao { get; set; }
        public string DataFormatada => DataRevisao.ToString("dd/MM/yyyy");
        public string HoraFormatada => DataRevisao.ToString("HH:mm");
        public string TipoRevisaoNome => TipoRevisao.ToString().Replace("Classico", "Clássico ").Replace("Ciclo", "Ciclo ");
    }

    // Modelo para exibir próxima prova
    public class ProximaProva
    {
        public DateTime DataProva { get; set; }
        public string Orgao { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public int DiasParaProva { get; set; }
        public string DataFormatada => DataProva.ToString("dd/MM/yyyy");
        public string DataExtenso => DataProva.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR"));
        public string TituloCompleto => $"{Orgao} - {Cargo}";
    }

    // Modelo para exibir progresso de conclusão dos assuntos
    public class AssuntoProgresso : INotifyPropertyChanged
    {
        private string _nome = string.Empty;
        private bool _concluido;
        private double _percentualConclusao;
        private int _totalAcertos;
        private int _totalErros;
        private double _horasEstudadas;

        public string Nome
        {
            get => _nome;
            set
            {
                _nome = value;
                OnPropertyChanged(nameof(Nome));
            }
        }

        public bool Concluido
        {
            get => _concluido;
            set
            {
                _concluido = value;
                OnPropertyChanged(nameof(Concluido));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public double PercentualConclusao
        {
            get => _percentualConclusao;
            set
            {
                _percentualConclusao = value;
                OnPropertyChanged(nameof(PercentualConclusao));
            }
        }

        public int TotalAcertos
        {
            get => _totalAcertos;
            set
            {
                _totalAcertos = value;
                OnPropertyChanged(nameof(TotalAcertos));
            }
        }

        public int TotalErros
        {
            get => _totalErros;
            set
            {
                _totalErros = value;
                OnPropertyChanged(nameof(TotalErros));
            }
        }

        public double HorasEstudadas
        {
            get => _horasEstudadas;
            set
            {
                _horasEstudadas = value;
                OnPropertyChanged(nameof(HorasEstudadas));
            }
        }

        public string StatusText => Concluido ? "Concluído" : "Em Andamento";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Modelo para exibir metas da semana
    public class MetasSemana
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string PeriodoFormatado => $"{DataInicio:dd/MM} - {DataFim:dd/MM}";
        
        // Metas
        public int MetaHoras { get; set; }
        public int MetaQuestoes { get; set; }
        public int MetaPaginas { get; set; }
        
        // Realizados
        public double HorasRealizadas { get; set; }
        public int QuestoesRealizadas { get; set; }
        public int PaginasRealizadas { get; set; }
        
        // Percentuais
        public double PercentualHoras => MetaHoras > 0 ? Math.Min((HorasRealizadas / MetaHoras) * 100, 100) : 0;
        public double PercentualQuestoes => MetaQuestoes > 0 ? Math.Min((QuestoesRealizadas / (double)MetaQuestoes) * 100, 100) : 0;
        public double PercentualPaginas => MetaPaginas > 0 ? Math.Min((PaginasRealizadas / (double)MetaPaginas) * 100, 100) : 0;
        
        // Status
        public bool HorasConcluida => HorasRealizadas >= MetaHoras;
        public bool QuestoesConcluida => QuestoesRealizadas >= MetaQuestoes;
        public bool PaginasConcluida => PaginasRealizadas >= MetaPaginas;
    }
}
