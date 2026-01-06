using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyMinder.Models
{
    public class Edital : IAuditable
    {
        public Edital()
        {
            EditalAssuntos = new List<EditalAssunto>();
            EditalCronogramas = new List<EditalCronograma>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Cargo { get; set; } = string.Empty;
        
        [Required]
        public string Orgao { get; set; } = string.Empty;
        
        [StringLength(10)]
        public string? Salario { get; set; }
        
        public int? VagasImediatas { get; set; }
        
        public int? VagasCadastroReserva { get; set; }
        
        public int? Concorrencia { get; set; }
        
        public int? Colocacao { get; set; }
        
        [StringLength(50)]
        public string? NumeroInscricao { get; set; }
        
        public int? AcertosProva { get; set; }
        
        public int? ErrosProva { get; set; }
        
        public int? BrancosProva { get; set; }
        
        public int? AnuladasProva { get; set; }
        
        public int? TipoProvaId { get; set; }
        
        public int? EscolaridadeId { get; set; }
        
        [NotMapped]
        public decimal? RendimentoProva
        {
            get
            {
                if (!AcertosProva.HasValue || !ErrosProva.HasValue)
                    return null;
                
                int total = AcertosProva.Value + ErrosProva.Value;
                if (total == 0)
                    return null;
                
                return Math.Round((decimal)AcertosProva.Value / total * 100, 2);
            }
        }

        /// <summary>
        /// Calcula o percentil da colocação do candidato em relação à concorrência.
        /// Indica quantos porcento da concorrência o usuário conseguiu superar.
        /// 100% = 1º lugar, 0% = último lugar
        /// </summary>
        [NotMapped]
        public decimal? ColocacaoPercentil
        {
            get
            {
                if (!Colocacao.HasValue || !Concorrencia.HasValue)
                    return null;
                
                if (Concorrencia.Value == 0)
                    return null;
                
                // Fórmula: (Concorrência - Colocação) / Concorrência * 100
                // Exemplo: Se há 100 candidatos e o usuário ficou em 1º lugar:
                // (100 - 1) / 100 * 100 = 99%
                // Se ficou em último (100º lugar):
                // (100 - 100) / 100 * 100 = 0%
                decimal percentil = (decimal)(Concorrencia.Value - Colocacao.Value) / Concorrencia.Value * 100;
                return Math.Round(percentil, 2);
            }
        }
        
        [ForeignKey("EscolaridadeId")]
        public virtual Escolaridade? Escolaridade { get; set; }
        
        [ForeignKey("TipoProvaId")]
        public virtual TiposProva? TiposProva { get; set; }
        
        public string? Banca { get; set; }
        
        public string? Area { get; set; }
        
        public string? Link { get; set; }
        
        [StringLength(10)]
        public string? ValorInscricao { get; set; }
        
        public int? FaseEditalId { get; set; }
        
        [ForeignKey("FaseEditalId")]
        public virtual FaseEdital? FaseEdital { get; set; }
        
        public bool ProvaDiscursiva { get; set; } = false;
        
        public bool ProvaTitulos { get; set; } = false;
        
        public bool ProvaTaf { get; set; } = false;
        
        public bool ProvaPratica { get; set; } = false;
        
        public bool BoletoPago { get; set; } = false;
        
        [Required]
        public long DataAberturaTicks { get; set; }
        
        [NotMapped]
        public DateTime DataAbertura
        {
            get => new DateTime(DataAberturaTicks);
            set => DataAberturaTicks = value.Ticks;
        }
        
        [Required]
        public long DataProvaTicks { get; set; }
        
        [NotMapped]
        public DateTime DataProva
        {
            get => new DateTime(DataProvaTicks);
            set => DataProvaTicks = value.Ticks;
        }
        
        public bool Arquivado { get; set; } = false;
        
        public int? Validade { get; set; }
        
        public long? DataHomologacaoTicks { get; set; }
        
        [NotMapped]
        public DateTime? DataHomologacao
        {
            get => DataHomologacaoTicks.HasValue ? new DateTime(DataHomologacaoTicks.Value) : null;
            set => DataHomologacaoTicks = value?.Ticks;
        }
        
        [NotMapped]
        public bool Encerrado
        {
            get => DataHomologacao.HasValue;
        }
        
        [NotMapped]
        public DateTime? ValidadeFim
        {
            get
            {
                if (!DataHomologacao.HasValue || !Validade.HasValue)
                    return null;
                
                return DataHomologacao.Value.AddMonths(Validade.Value);
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
        
        // Relacionamentos
        public virtual ICollection<EditalAssunto> EditalAssuntos { get; set; }
        public virtual ICollection<EditalCronograma> EditalCronogramas { get; set; }
        
        // Propriedades calculadas
        [NotMapped]
        public decimal ProgressoGeral 
        { 
            get 
            { 
                if (EditalAssuntos == null || EditalAssuntos.Count == 0) return 0;
                var assuntosConcluidos = EditalAssuntos.Count(ea => ea.Assunto?.Concluido == true);
                return Math.Round((decimal)assuntosConcluidos / EditalAssuntos.Count * 100, 2);
            } 
        }
        
        [NotMapped]
        public int TotalPaginasLidas
        {
            get
            {
                if (EditalAssuntos == null || EditalAssuntos.Count == 0) return 0;
                
                int totalPaginas = 0;
                foreach (var editalAssunto in EditalAssuntos)
                {
                    if (editalAssunto.Assunto?.Estudos != null)
                    {
                        totalPaginas += editalAssunto.Assunto.Estudos
                            .Where(e => e.DataTicks >= DataAberturaTicks && e.DataTicks <= DataProvaTicks)
                            .Sum(e => e.TotalPaginas);
                    }
                }
                return totalPaginas;
            }
        }
        
        [NotMapped]
        public string StatusDinamico
        {
            get
            {
                // Verificar se informações básicas estão preenchidas
                bool temInformacoesBasicas = !string.IsNullOrWhiteSpace(Cargo) && !string.IsNullOrWhiteSpace(Orgao);
                
                // Verificar se tem assuntos vinculados
                bool temAssuntos = EditalAssuntos != null && EditalAssuntos.Count > 0;
                
                // Verificar se tem estudos registrados
                bool temEstudos = false;
                if (EditalAssuntos != null)
                {
                    foreach (var ea in EditalAssuntos)
                    {
                        if (ea.Assunto?.Estudos != null && ea.Assunto.Estudos.Count > 0)
                        {
                            temEstudos = true;
                            break;
                        }
                    }
                }
                
                // Calcular progresso
                decimal progresso = ProgressoGeral;
                
                // Lógica de status
                if (!temInformacoesBasicas)
                    return "Incompleto";
                    
                if (!temAssuntos)
                    return "Sem Assuntos";
                    
                if (!temEstudos)
                    return "Sem Estudos";
                    
                if (progresso >= 100)
                    return "Concluído";
                    
                if (progresso >= 50)
                    return "Em Progresso";
                    
                return "Iniciado";
            }
        }
        
        [NotMapped]
        public string Nome
        {
            get => $"{Orgao} - {Cargo} - {DataAbertura.Year}";
        }
        
        // Métodos auxiliares
        public void AtualizarDataModificacao()
        {
            DataModificacao = DateTime.UtcNow;
        }

        public override string ToString() => Nome;
    }
}
