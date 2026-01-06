using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyMinder.Models
{
    public class EditalCronograma : IAuditable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int EditalId { get; set; }
        
        [ForeignKey("EditalId")]
        public virtual Edital Edital { get; set; } = null!;
        
        private string _evento = string.Empty;
        [Required(ErrorMessage = "O nome do evento é obrigatório.")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "O evento deve ter entre 1 e 255 caracteres.")]
        public string Evento
        {
            get => _evento;
            set
            {
                if (_evento != value)
                {
                    _evento = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Evento)));
                }
            }
        }
        
        private long _dataEventoTicks;
        [Required]
        public long DataEventoTicks
        {
            get => _dataEventoTicks;
            set
            {
                if (_dataEventoTicks != value)
                {
                    _dataEventoTicks = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataEventoTicks)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataEvento)));
                }
            }
        }
        
        [NotMapped]
        public DateTime DataEvento
        {
            get => new DateTime(DataEventoTicks);
            set => DataEventoTicks = value.Ticks;
        }
        
        private bool _concluido = false;
        public bool Concluido
        {
            get => _concluido;
            set
            {
                if (_concluido != value)
                {
                    _concluido = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Concluido)));
                }
            }
        }
        
        private bool _ignorado = false;
        public bool Ignorado
        {
            get => _ignorado;
            set
            {
                if (_ignorado != value)
                {
                    _ignorado = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Ignorado)));
                }
            }
        }
        
        [NotMapped]
        private bool _isEditing = false;
        
        [NotMapped]
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEditing)));
                }
            }
        }
        
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
        
        // Métodos auxiliares
        public void AtualizarDataModificacao()
        {
            DataModificacao = DateTime.UtcNow;
        }
    }
}
