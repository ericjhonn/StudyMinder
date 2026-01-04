using StudyMinder.Models;
using System;

namespace StudyMinder.Services
{
    public class AuditoriaService
    {
        public void AtualizarAuditoria(IAuditable entidade, bool isNew)
        {
            var agora = DateTime.UtcNow;
            
            if (isNew)
            {
                entidade.DataCriacao = agora;
            }
            
            entidade.DataModificacao = agora;
        }
    }
}
