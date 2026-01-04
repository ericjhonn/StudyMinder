namespace StudyMinder.ViewModels
{
    /// <summary>
    /// Interface para ViewModels que suportam edição com rastreamento de mudanças
    /// </summary>
    public interface IEditableViewModel
    {
        /// <summary>
        /// Indica se há alterações não salvas
        /// </summary>
        bool HasUnsavedChanges { get; }

        /// <summary>
        /// Chamado quando o usuário tenta sair da view com alterações não salvas
        /// Retorna true se a navegação deve ser cancelada, false caso contrário
        /// </summary>
        Task<bool> OnViewUnloadingAsync();
    }
}
