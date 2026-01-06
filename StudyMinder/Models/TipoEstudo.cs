using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyMinder.Models
{
    public class TipoEstudo : IAuditable
    {
        public TipoEstudo()
        {
            Estudos = new List<Estudo>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Nome { get; set; } = string.Empty;
        
        [Required]
        public bool Ativo { get; set; } = true;
        
        [Required]
        public long DataCriacaoTicks { get; set; } = DateTime.UtcNow.Ticks;
        
        [NotMapped]
        public DateTime DataCriacao
        {
            get => new DateTime(DataCriacaoTicks);
            set => DataCriacaoTicks = value.Ticks;
        }
        
        [NotMapped]
        public DateTime DataModificacao { get; set; } = DateTime.UtcNow;
        
        // Relacionamentos
        public virtual ICollection<Estudo> Estudos { get; set; }
        
        // Métodos auxiliares
        public void AtualizarDataModificacao()
        {
            DataModificacao = DateTime.UtcNow;
        }

        public override string ToString() => Nome;
    }
}
