using CommunityToolkit.Mvvm.ComponentModel;
using StudyMinder.Models;

namespace StudyMinder.ViewModels
{
    /// <summary>
    /// Wrapper para um Assunto que permite seleção em modo de edição em massa.
    /// Implementa INotifyPropertyChanged para permitir binding com CheckBox.
    /// </summary>
    public partial class AssuntoSelecionavel : ObservableObject
    {
        [ObservableProperty]
        private bool isSelected;

        public Assunto Assunto { get; }
        
        /// <summary>
        /// Callback invocado quando IsSelected muda, para sincronizar com o dicionário do ViewModel
        /// </summary>
        private Action<int, bool>? _onSelectionChanged;

        /// <summary>
        /// Cria uma nova instância de AssuntoSelecionavel.
        /// </summary>
        /// <param name="assunto">O assunto a ser envolvido.</param>
        /// <param name="isSelected">Define se o assunto está inicialmente selecionado.</param>
        /// <param name="onSelectionChanged">Callback para sincronizar seleção com o ViewModel</param>
        public AssuntoSelecionavel(Assunto assunto, bool isSelected = false, Action<int, bool>? onSelectionChanged = null)
        {
            Assunto = assunto;
            _onSelectionChanged = onSelectionChanged;
            IsSelected = isSelected;
        }
        
        /// <summary>
        /// Intercepta mudanças em IsSelected para sincronizar com o dicionário
        /// </summary>
        partial void OnIsSelectedChanged(bool value)
        {
            _onSelectionChanged?.Invoke(Assunto.Id, value);
        }
    }
}
