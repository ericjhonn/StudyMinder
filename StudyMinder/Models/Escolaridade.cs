using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyMinder.Models
{
    public class Escolaridade
    {
        public Escolaridade()
        {
            Editais = new List<Edital>();
        }

        [Key]
        public int Id { get; set; }
        
        [StringLength(100)]
        [Column("Escolaridade")]
        public string Nome { get; set; } = string.Empty;
        
        // Relacionamentos
        public virtual ICollection<Edital> Editais { get; set; }

        public override string ToString() => Nome;
    }
}
