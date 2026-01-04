using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyMinder.Models
{
    [Table("CicloEstudo")]
    public class CicloEstudo
    {
        [Key]
        [ForeignKey("Assunto")]
        public int AssuntoId { get; set; }
        
        public int Ordem { get; set; } // Sequencial: 1, 2, 3...
        
        [Column("Duracao")]
        public long DuracaoTicks { get; set; } // Timebox em Ticks

        [NotMapped]
        public int DuracaoMinutos 
        { 
            get => (int)TimeSpan.FromTicks(DuracaoTicks).TotalMinutes;
            set => DuracaoTicks = TimeSpan.FromMinutes(value).Ticks;
        }

        [NotMapped]
        public Estudo? UltimoEstudo { get; set; }

        public virtual Assunto Assunto { get; set; } = null!;
    }
}