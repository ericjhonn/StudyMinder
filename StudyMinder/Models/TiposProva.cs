using System.ComponentModel.DataAnnotations;

namespace StudyMinder.Models
{
    public class TiposProva
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;
        
        // Relacionamentos
        public virtual ICollection<Edital> Editais { get; set; } = new List<Edital>();

        public override string ToString() => Nome;
    }
}
