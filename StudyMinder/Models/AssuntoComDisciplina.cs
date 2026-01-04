using StudyMinder.Models;

namespace StudyMinder.Models
{
    /// <summary>
    /// Classe auxiliar para representar um assunto na fila cíclica com informações adicionais
    /// </summary>
    public class AssuntoComDisciplina
    {
        public Assunto Assunto { get; set; } = null!;
        public string Disciplina { get; set; } = string.Empty;
        public int PosicaoFila { get; set; }
        public DateTime? DataUltimoEstudo { get; set; }
        public bool IsPrimeiroDaFila { get; set; }
        
        public string StatusUltimoEstudo
        {
            get
            {
                if (DataUltimoEstudo == null || DataUltimoEstudo.Value.Ticks == 0)
                    return "Nunca estudado";
                
                var dias = (DateTime.Today - DataUltimoEstudo.Value).Days;
                return dias switch
                {
                    0 => "Hoje",
                    1 => "Ontem",
                    < 7 => $"Há {dias} dias",
                    < 30 => $"Há {dias / 7} semanas",
                    _ => $"Há {dias / 30} meses"
                };
            }
        }
    }
}
