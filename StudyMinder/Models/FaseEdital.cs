using System.ComponentModel.DataAnnotations;

namespace StudyMinder.Models
{
    public class FaseEdital
    {
        public FaseEdital()
        {
            Editais = new List<Edital>();
        }

        [Key]
        public int Id { get; set; }
        
        [StringLength(100)]
        public string Fase { get; set; } = string.Empty;
        
        // Relacionamentos
        public virtual ICollection<Edital> Editais { get; set; }

        public override string ToString() => Fase;
    }
}
