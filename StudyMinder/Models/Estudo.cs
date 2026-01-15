using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyMinder.Models
{
    public class Estudo : IAuditable
    {
        public Estudo()
        {
            
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        public int TipoEstudoId { get; set; }
        
        [ForeignKey("TipoEstudoId")]
        public virtual TipoEstudo TipoEstudo { get; set; } = null!;
        
        [Required]
        public int AssuntoId { get; set; }
        
        [ForeignKey("AssuntoId")]
        public virtual Assunto Assunto { get; set; } = null!;
        
        [Required]
        public long DataTicks { get; set; } = DateTime.Now.Ticks;
        
        [NotMapped]
        public DateTime Data
        {
            get => new DateTime(DataTicks);
            set => DataTicks = value.Ticks;
        }
        
        [Required]
        public long DuracaoTicks { get; set; }
        
        [NotMapped]
        public TimeSpan Duracao
        {
            get => TimeSpan.FromTicks(DuracaoTicks);
            set => DuracaoTicks = value.Ticks;
        }
        
        [Required]
        public int Acertos { get; set; } = 0;
        
        [Required]
        public int Erros { get; set; } = 0;
        
        [Required]
        public int PaginaInicial { get; set; } = 0;
        
        [Required]
        public int PaginaFinal { get; set; } = 0;
        
        public string? Material { get; set; }
        
        public string? Professor { get; set; }
        
        public string? Topicos { get; set; }
        
        public string? Comentarios { get; set; }
        
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
                        
        // Propriedades calculadas
        [NotMapped]
        public int TotalQuestoes => Acertos + Erros;
        
        [NotMapped]
        public double RendimentoPercentual
        {
            get
            {
                if (TotalQuestoes == 0) return 0;
                return Math.Round((double)Acertos / TotalQuestoes * 100, 2);
            }
        }
        
        [NotMapped]
        public int TotalPaginas => (PaginaFinal > 0 && PaginaInicial > 0) ? (PaginaFinal - PaginaInicial + 1) : 0;
        
        [NotMapped]
        public string HorasEstudadas
        {
            get
            {
                var timespan = TimeSpan.FromTicks(DuracaoTicks);
                var horas = (int)timespan.TotalHours;
                var minutos = timespan.Minutes;
                
                return $"{horas}h{minutos:D2}";
            }
        }
        
        // Métodos auxiliares
        public void AtualizarDataModificacao()
        {
            DataModificacao = DateTime.UtcNow;
        }
    }
}
