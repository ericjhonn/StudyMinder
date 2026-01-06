using System;

namespace StudyMinder.Models
{
    public interface IAuditable
    {
        DateTime DataCriacao { get; set; }
        DateTime DataModificacao { get; set; }
    }
}
