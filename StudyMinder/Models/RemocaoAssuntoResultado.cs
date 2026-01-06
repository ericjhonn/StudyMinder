namespace StudyMinder.Models
{
    /// <summary>
    /// Representa o resultado da operação de remoção de um assunto no diálogo RemoverAssuntoDialog.
    /// Contém informações sobre como o assunto deve ser removido: em cascata ou com movimentação de estudos.
    /// </summary>
    public class RemocaoAssuntoResultado
    {
        /// <summary>
        /// ID do assunto a ser removido.
        /// </summary>
        public int AssuntoId { get; set; }

        /// <summary>
        /// Indica se a remoção será em cascata (remover todos os estudos).
        /// Se false, significa que os estudos serão movidos para outro assunto.
        /// </summary>
        public bool RemoverEmCascata { get; set; }

        /// <summary>
        /// ID do assunto de destino para movimentação de estudos.
        /// Usado apenas quando RemoverEmCascata é false.
        /// </summary>
        public int? AssuntoDestinoId { get; set; }

        /// <summary>
        /// ID da disciplina de destino.
        /// Usado apenas quando RemoverEmCascata é false.
        /// </summary>
        public int? DisciplinaDestinoId { get; set; }

        /// <summary>
        /// Quantidade de estudos que serão afetados pela operação.
        /// </summary>
        public int TotalEstudos { get; set; }

        public RemocaoAssuntoResultado() { }

        public RemocaoAssuntoResultado(int assuntoId, bool removerEmCascata, int totalEstudos)
        {
            AssuntoId = assuntoId;
            RemoverEmCascata = removerEmCascata;
            TotalEstudos = totalEstudos;
        }

        public RemocaoAssuntoResultado(int assuntoId, int assuntoDestinoId, int disciplinaDestinoId, int totalEstudos)
        {
            AssuntoId = assuntoId;
            RemoverEmCascata = false;
            AssuntoDestinoId = assuntoDestinoId;
            DisciplinaDestinoId = disciplinaDestinoId;
            TotalEstudos = totalEstudos;
        }
    }
}
