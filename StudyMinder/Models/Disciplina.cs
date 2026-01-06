using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.ObjectModel;

namespace StudyMinder.Models
{
    public class Disciplina : IAuditable
    {
        public Disciplina()
        {
            Assuntos = new ObservableCollection<Assunto>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "O nome da disciplina é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;
        
        [Required]
        [StringLength(7)]
        [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Formato de cor inválido. Use o formato hexadecimal (#RRGGBB).")]
        public string Cor { get; set; } = "#3498db";
        
        public bool Arquivado { get; set; }
        
        private double? _progressoCache;
        private DateTime _lastProgressCalculation = DateTime.MinValue;
        
        [NotMapped]
        public double Progresso 
        { 
            get
            {
                // Recalculate if cache is invalid (older than 5 seconds)
                if (_progressoCache == null || (DateTime.Now - _lastProgressCalculation).TotalSeconds > 5)
                {
                    if (Assuntos == null || !Assuntos.Any())
                        return 0;

                    var assuntosNaoArquivados = Assuntos.Where(a => !a.Arquivado).ToList();
                    _progressoCache = !assuntosNaoArquivados.Any() 
                        ? 0 
                        : (double)assuntosNaoArquivados.Count(a => a.Concluido) / assuntosNaoArquivados.Count * 100;
                        
                    _lastProgressCalculation = DateTime.Now;
                }
                return _progressoCache.Value;
            }
        }
        
        public void InvalidateProgressCache()
        {
            _progressoCache = null;
        }
        
        [NotMapped]
        public double Rendimento
        {
            get
            {
                if (Assuntos == null || !Assuntos.Any())
                    return 0;

                var assuntosNaoArquivados = Assuntos.Where(a => !a.Arquivado).ToList();
                if (!assuntosNaoArquivados.Any())
                    return 0;

                var totalAcertos = assuntosNaoArquivados.Sum(a => a.TotalAcertos);
                var totalTentativas = totalAcertos + assuntosNaoArquivados.Sum(a => a.TotalErros);

                return totalTentativas > 0 ? (double)totalAcertos / totalTentativas * 100 : 0;
            }
        }
        
        [NotMapped]
        public int TotalAcertos => Assuntos?.Sum(a => a.TotalAcertos) ?? 0;
        
        [NotMapped]
        public int TotalErros => Assuntos?.Sum(a => a.TotalErros) ?? 0;
        
        [NotMapped]
        public int TotalAssuntos => Assuntos?.Count(a => !a.Arquivado) ?? 0;
        
        [NotMapped]
        public string HorasEstudadas
        {
            get
            {
                if (Assuntos == null || !Assuntos.Any())
                    return "0h00";

                var assuntosNaoArquivados = Assuntos.Where(a => !a.Arquivado).ToList();
                if (!assuntosNaoArquivados.Any())
                    return "0h00";

                var totalTicks = assuntosNaoArquivados.Sum(a => a.Estudos?.Sum(e => e.DuracaoTicks) ?? 0);
                var timespan = TimeSpan.FromTicks(totalTicks);
                var horas = (int)timespan.TotalHours;
                var minutos = timespan.Minutes;
                
                return $"{horas}h{minutos:D2}";
            }
        }
        
        [NotMapped]
        public int TotalQuestoes => Assuntos?.Where(a => !a.Arquivado).Sum(a => a.TotalAcertos + a.TotalErros) ?? 0;
        
        [NotMapped]
        public int TotalPaginasLidas => Assuntos?.Where(a => !a.Arquivado).Sum(a => a.Estudos?.Sum(e => e.TotalPaginas) ?? 0) ?? 0;

        [Required]
        public long DataCriacaoTicks { get; set; } = DateTime.UtcNow.Ticks;
        
        [Required]
        public long DataModificacaoTicks { get; set; } = DateTime.UtcNow.Ticks;
        
        [NotMapped]
        public DateTime DataCriacao
        {
            get => new DateTime(DataCriacaoTicks);
            set => DataCriacaoTicks = value.Ticks;
        }
        
        [NotMapped]
        public DateTime DataModificacao
        {
            get => new DateTime(DataModificacaoTicks);
            set => DataModificacaoTicks = value.Ticks;
        }
        
        // Relacionamentos
        public virtual ICollection<Assunto> Assuntos { get; set; }

        // Métodos auxiliares
        public void AtualizarDataModificacao()
        {
            DataModificacao = DateTime.UtcNow;
        }

        public override string ToString() => Nome;
    }
}
