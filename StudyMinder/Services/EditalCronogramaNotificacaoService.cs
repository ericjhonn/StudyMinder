using StudyMinder.Models;

namespace StudyMinder.Services
{
    /// <summary>
    /// Serviço de notificação para eventos de cronograma de editais.
    /// Permite que diferentes módulos sejam notificados quando um cronograma é adicionado, atualizado ou removido.
    /// </summary>
    public class EditalCronogramaNotificacaoService
    {
        // Eventos que podem ser subscritos
        public event EventHandler<EditalCronogramaEventArgs>? CronogramaAdicionado;
        public event EventHandler<EditalCronogramaEventArgs>? CronogramaAtualizado;
        public event EventHandler<EditalCronogramaEventArgs>? CronogramaRemovido;

        /// <summary>
        /// Notifica que um cronograma foi adicionado
        /// </summary>
        public void NotificarCronogramaAdicionado(EditalCronograma cronograma)
        {
            CronogramaAdicionado?.Invoke(this, new EditalCronogramaEventArgs { Cronograma = cronograma });
        }

        /// <summary>
        /// Notifica que um cronograma foi atualizado
        /// </summary>
        public void NotificarCronogramaAtualizado(EditalCronograma cronograma)
        {
            CronogramaAtualizado?.Invoke(this, new EditalCronogramaEventArgs { Cronograma = cronograma });
        }

        /// <summary>
        /// Notifica que um cronograma foi removido
        /// </summary>
        public void NotificarCronogramaRemovido(EditalCronograma cronograma)
        {
            CronogramaRemovido?.Invoke(this, new EditalCronogramaEventArgs { Cronograma = cronograma });
        }
    }

    /// <summary>
    /// Argumentos do evento de cronograma
    /// </summary>
    public class EditalCronogramaEventArgs : EventArgs
    {
        public EditalCronograma? Cronograma { get; set; }
    }
}
