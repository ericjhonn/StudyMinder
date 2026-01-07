using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyMinder.Models
{
    public enum TipoRevisaoEnum
    {
        Classico24h = 1,
        Classico7d = 2,
        Classico30d = 3,
        Classico90d = 4,
        Classico120d = 5,
        Classico180d = 6,
        Ciclo42 = 10,
        Ciclico = 20
    }

    public class Revisao : IAuditable
    {
        public Revisao()
        {
            
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public int EstudoOrigemId { get; set; }

        [ForeignKey("EstudoOrigemId")]
        public virtual Estudo EstudoOrigem { get; set; } = null!;

        public int? EstudoRealizadoId { get; set; }

        [ForeignKey("EstudoRealizadoId")]
        public virtual Estudo? EstudoRealizado { get; set; }

        [Required]
        public TipoRevisaoEnum TipoRevisao { get; set; }

        [Required]
        public long DataProgramadaTicks { get; set; }

        [NotMapped]
        public DateTime DataProgramada
        {
            get => new DateTime(DataProgramadaTicks);
            set => DataProgramadaTicks = value.Ticks;
        }

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

        [NotMapped]
        public bool EstaPendente => EstudoRealizadoId == null;

        [NotMapped]
        public bool EstaConcluida => EstudoRealizadoId != null;

        // Métodos auxiliares
        public void AtualizarDataModificacao()
        {
            DataModificacao = DateTime.UtcNow;
        }
    }
}
