using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.ObjectModel;

namespace StudyMinder.Models
{
    public class Assunto : IAuditable, INotifyPropertyChanged
    {
        public Assunto()
        {
            Estudos = new ObservableCollection<Estudo>();
            EditalAssuntos = new ObservableCollection<EditalAssunto>();
        }

        [Key]
        public int Id { get; set; }
        
        private string _nome = string.Empty;
        
        [Required(ErrorMessage = "O nome do assunto é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Nome 
        { 
            get => _nome; 
            set 
            { 
                if (_nome != value)
                {
                    _nome = value;
                    OnPropertyChanged(nameof(Nome));
                }
            } 
        }
        
        public string? CadernoQuestoes { get; set; }
        
        private bool _concluido;
        public bool Concluido 
        { 
            get => _concluido; 
            set 
            { 
                if (_concluido != value)
                {
                    _concluido = value;
                    OnPropertyChanged(nameof(Concluido));
                    
                    // Invalida o cache de progresso da disciplina
                    Disciplina?.InvalidateProgressCache();
                }
            } 
        }
        
        private bool _arquivado;
        public bool Arquivado 
        { 
            get => _arquivado; 
            set 
            { 
                if (_arquivado != value)
                {
                    _arquivado = value;
                    OnPropertyChanged(nameof(Arquivado));
                    
                    // Invalida o cache de progresso da disciplina
                    Disciplina?.InvalidateProgressCache();
                }
            } 
        }
        
        [NotMapped]
        public string HorasEstudadas
        {
            get
            {
                if (Estudos == null || !Estudos.Any())
                    return "0h00";

                var totalTicks = Estudos.Sum(e => e.DuracaoTicks);
                var timespan = TimeSpan.FromTicks(totalTicks);
                var horas = (int)timespan.TotalHours;
                var minutos = timespan.Minutes;
                
                return $"{horas}h{minutos:D2}";
            }
        }
        
        [NotMapped]
        public int TotalAcertos => Estudos?.Sum(e => e.Acertos) ?? 0;
        
        [NotMapped]
        public int TotalErros => Estudos?.Sum(e => e.Erros) ?? 0;
        
        [NotMapped]
        public double Rendimento
        {
            get
            {
                var totalTentativas = TotalAcertos + TotalErros;
                return totalTentativas > 0 ? (double)TotalAcertos / totalTentativas * 100 : 0;
            }
        }
        
        private int? _progresso;
        
        [NotMapped]
        public int? Progresso 
        { 
            get => _progresso; 
            set 
            { 
                if (_progresso != value)
                {
                    _progresso = value;
                    OnPropertyChanged(nameof(Progresso));
                }
            } 
        }
        
        [Required]
        public int DisciplinaId { get; set; }
        
        [Required]
        public long DataCriacaoTicks { get; set; } = DateTime.UtcNow.Ticks;
        
        [Required]
        public long DataModificacaoTicks { get; set; } = DateTime.UtcNow.Ticks;
        
        [NotMapped]
        public DateTime DataCriacao
        {
            get => new DateTime(DataCriacaoTicks);
            set => DataCriacaoTicks = value.Ticks;
        }
        
        [NotMapped]
        public DateTime DataModificacao
        {
            get => new DateTime(DataModificacaoTicks);
            set => DataModificacaoTicks = value.Ticks;
        }
        
        // Relacionamentos
        [ForeignKey("DisciplinaId")]
        public virtual Disciplina Disciplina { get; set; } = null!;
        
        public virtual ICollection<Estudo> Estudos { get; set; }
        public virtual ICollection<EditalAssunto> EditalAssuntos { get; set; }

        private bool _isEditing;
        
        [NotMapped]
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Métodos auxiliares
        public void MarcarComoConcluido()
        {
            if (!Concluido)
            {
                Concluido = true;
                DataModificacao = DateTime.UtcNow;
            }
        }

        public void MarcarComoNaoConcluido()
        {
            if (Concluido)
            {
                Concluido = false;
                DataModificacao = DateTime.UtcNow;
            }
        }

        public void AtualizarDataModificacao()
        {
            DataModificacao = DateTime.UtcNow;
        }

        [NotMapped]
        public DateTime? DataUltimoEstudo { get; set; }

        private string _statusNoCiclo = "";

        [NotMapped]
        public string StatusNoCiclo
        {
            get => _statusNoCiclo;
            set
            {
                if (_statusNoCiclo != value)
                {
                    _statusNoCiclo = value;
                    OnPropertyChanged(nameof(StatusNoCiclo));
                }
            }
        }

        public override string ToString() => Nome;
    }
}
