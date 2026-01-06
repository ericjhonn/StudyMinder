using StudyMinder.Models;

namespace StudyMinder.Services
{
    /// <summary>
    /// Serviço de notificação para eventos de estudo.
    /// Permite que diferentes módulos sejam notificados quando um estudo é adicionado, atualizado ou removido.
    /// </summary>
    public class EstudoNotificacaoService
    {
        // Eventos que podem ser subscritos
        public event EventHandler<EstudoEventArgs>? EstudoAdicionado;
        public event EventHandler<EstudoEventArgs>? EstudoAtualizado;
        public event EventHandler<EstudoEventArgs>? EstudoRemovido;

        /// <summary>
        /// Notifica que um estudo foi adicionado
        /// </summary>
        public void NotificarEstudoAdicionado(Estudo estudo)
        {
            EstudoAdicionado?.Invoke(this, new EstudoEventArgs { Estudo = estudo });
        }

        /// <summary>
        /// Notifica que um estudo foi atualizado
        /// </summary>
        public void NotificarEstudoAtualizado(Estudo estudo)
        {
            EstudoAtualizado?.Invoke(this, new EstudoEventArgs { Estudo = estudo });
        }

        /// <summary>
        /// Notifica que um estudo foi removido
        /// </summary>
        public void NotificarEstudoRemovido(Estudo estudo)
        {
            EstudoRemovido?.Invoke(this, new EstudoEventArgs { Estudo = estudo });
        }
    }

    /// <summary>
    /// Argumentos do evento de estudo
    /// </summary>
    public class EstudoEventArgs : EventArgs
    {
        public Estudo? Estudo { get; set; }
    }
}
