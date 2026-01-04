using CommunityToolkit.Mvvm.ComponentModel;
using StudyMinder.Services;

namespace StudyMinder.ViewModels
{
    /// <summary>
    /// ViewModel para a janela de MessageBox customizada.
    /// Contém apenas estado de exibição e resultado escolhido pelo usuário.
    /// </summary>
    public class MessageBoxViewModel : ObservableObject
    {
        private string _title = string.Empty;
        private string _message = string.Empty;
        private MessageType _messageType;
        private MessageBoxButtons _buttons;
        private ToastMessageBoxResult _result;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public MessageType MessageType
        {
            get => _messageType;
            set => SetProperty(ref _messageType, value);
        }

        public MessageBoxButtons Buttons
        {
            get => _buttons;
            set => SetProperty(ref _buttons, value);
        }

        public ToastMessageBoxResult Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }

        public MessageBoxViewModel(string title, string message, MessageType messageType = MessageType.Info, MessageBoxButtons buttons = MessageBoxButtons.Ok)
        {
            Title = title;
            Message = message;
            MessageType = messageType;
            Buttons = buttons;
            Result = ToastMessageBoxResult.None;
        }
    }
}
