using System.ComponentModel.DataAnnotations;

namespace StudyMinder.Models
{
    public class StatusEdital
    {
        [Key]
        public int Id { get; set; }
        
        [StringLength(100)]
        public string Status { get; set; } = string.Empty;
    }
}
