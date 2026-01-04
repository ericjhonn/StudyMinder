namespace StudyMinder.Services
{
    /// <summary>
    /// Interface para o serviço de notificações do StudyMinder
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Configurações do sistema de notificações
        /// </summary>
        NotificationSettings Settings { get; }

        // Métodos para Toasts (não-bloqueantes)
        void ShowSuccess(string title, string message, int? durationMs = null);
        void ShowError(string title, string message, int? durationMs = null);
        void ShowWarning(string title, string message, int? durationMs = null);
        void ShowInfo(string title, string message, int? durationMs = null);
        void ShowToast(string title, string message, MessageType messageType = MessageType.Info, int? durationMs = null);

        // Métodos para MessageBox (bloqueantes)
        ToastMessageBoxResult ShowMessageBox(string title, string message, MessageType messageType = MessageType.Info, MessageBoxButtons buttons = MessageBoxButtons.Ok);
        ToastMessageBoxResult ShowConfirmation(string title, string message);
        void ShowErrorBox(string title, string message);
        void ShowSuccessBox(string title, string message);

        // Métodos de gerenciamento
        void ClearAllToasts();
        void SetSettings(NotificationSettings settings);
    }
}
