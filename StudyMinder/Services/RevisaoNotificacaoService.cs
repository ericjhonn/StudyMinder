using StudyMinder.Models;

namespace StudyMinder.Services
{
    /// <summary>
    /// Serviço de notificação para eventos de revisão.
    /// Permite que diferentes módulos sejam notificados quando uma revisão é adicionada, atualizada ou removida.
    /// </summary>
    public class RevisaoNotificacaoService
    {
        // Eventos que podem ser subscritos
        public event EventHandler<RevisaoEventArgs>? RevisaoAdicionada;
        public event EventHandler<RevisaoEventArgs>? RevisaoAtualizada;
        public event EventHandler<RevisaoEventArgs>? RevisaoRemovida;
        public event EventHandler<RevisaoCicloAtivoEventArgs>? AssuntoAdicionadoAoCiclo;
        public event EventHandler<RevisaoCicloAtivoEventArgs>? AssuntoRemovidoDoCiclo;

        /// <summary>
        /// Notifica que uma revisão foi adicionada
        /// </summary>
        public void NotificarRevisaoAdicionada(Revisao revisao)
        {
            RevisaoAdicionada?.Invoke(this, new RevisaoEventArgs { Revisao = revisao });
        }

        /// <summary>
        /// Notifica que uma revisão foi atualizada
        /// </summary>
        public void NotificarRevisaoAtualizada(Revisao revisao)
        {
            RevisaoAtualizada?.Invoke(this, new RevisaoEventArgs { Revisao = revisao });
        }

        /// <summary>
        /// Notifica que uma revisão foi removida
        /// </summary>
        public void NotificarRevisaoRemovida(Revisao revisao)
        {
            RevisaoRemovida?.Invoke(this, new RevisaoEventArgs { Revisao = revisao });
        }

        /// <summary>
        /// Notifica que um assunto foi adicionado ao ciclo ativo
        /// </summary>
        public void NotificarAssuntoAdicionadoAoCiclo(int assuntoId)
        {
            AssuntoAdicionadoAoCiclo?.Invoke(this, new RevisaoCicloAtivoEventArgs { AssuntoId = assuntoId });
        }

        /// <summary>
        /// Notifica que um assunto foi removido do ciclo ativo
        /// </summary>
        public void NotificarAssuntoRemovidoDoCiclo(int assuntoId)
        {
            AssuntoRemovidoDoCiclo?.Invoke(this, new RevisaoCicloAtivoEventArgs { AssuntoId = assuntoId });
        }
    }

    /// <summary>
    /// Argumentos do evento de revisão
    /// </summary>
    public class RevisaoEventArgs : EventArgs
    {
        public Revisao? Revisao { get; set; }
    }

    /// <summary>
    /// Argumentos do evento de ciclo ativo
    /// </summary>
    public class RevisaoCicloAtivoEventArgs : EventArgs
    {
        public int AssuntoId { get; set; }
    }
}
