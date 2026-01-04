using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyMinder.Models
{
    public class RevisaoCicloAtivo
    {
        public RevisaoCicloAtivo()
        {
        }

        [Key]
        [Required]
        public int AssuntoId { get; set; }

        [ForeignKey("AssuntoId")]
        public virtual Assunto Assunto { get; set; } = null!;

        [Required]
        [Column("DataInclusao")]
        public long DataInclusaoTicks { get; set; } = DateTime.UtcNow.Ticks;

        [NotMapped]
        public DateTime DataInclusao
        {
            get => new DateTime(DataInclusaoTicks);
            set => DataInclusaoTicks = value.Ticks;
        }
    }
}
