namespace StudyMinder.ViewModels
{
    /// <summary>
    /// Interface para ViewModels que precisam ser recarregados após navegação de volta.
    /// Implementado por ViewModels que gerenciam listas de dados (Disciplinas, Estudos, Editais, etc.)
    /// </summary>
    public interface IRefreshable
    {
        /// <summary>
        /// Recarrega os dados do ViewModel.
        /// Chamado automaticamente quando retornando de uma view de edição.
        /// </summary>
        void RefreshData();
    }
}
