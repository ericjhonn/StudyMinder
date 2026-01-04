using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyMinder.Models
{
    public class EditalAssunto
    {
        [Required]
        public int EditalId { get; set; }
        
        [ForeignKey("EditalId")]
        public virtual Edital Edital { get; set; } = null!;
        
        [Required]
        public int AssuntoId { get; set; }
        
        [ForeignKey("AssuntoId")]
        public virtual Assunto Assunto { get; set; } = null!;
    }
}
