using System;
using System.Globalization;
using System.Text;

namespace StudyMinder.Utils
{
    /// <summary>
    /// Classe auxiliar para normalizar strings removendo acentos e caracteres especiais
    /// </summary>
    public static class StringNormalizationHelper
    {
        /// <summary>
        /// Remove acentos e caracteres especiais de uma string
        /// Exemplo: "Português" → "Portugues", "São Paulo" → "Sao Paulo", "Função" → "Funcao"
        /// </summary>
        /// <param name="text">Texto a ser normalizado</param>
        /// <returns>Texto normalizado sem acentos</returns>
        public static string RemoveAccents(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Normalizar para NFD (decomposição)
            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder result = new StringBuilder();

            foreach (char c in normalized)
            {
                // Obter categoria Unicode do caractere
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                
                // Se não for marca diacrítica (acento), adicionar ao resultado
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            // Normalizar de volta para NFC
            string result_str = result.ToString().Normalize(NormalizationForm.FormC);
            
            // Remover caracteres especiais específicos como ç, ~, ^, ´, etc.
            result_str = result_str.Replace("ç", "c");
            result_str = result_str.Replace("Ç", "C");
            
            return result_str;
        }

        /// <summary>
        /// Verifica se um texto contém um termo de pesquisa, ignorando acentos e maiúsculas
        /// </summary>
        /// <param name="text">Texto a ser pesquisado</param>
        /// <param name="searchTerm">Termo de pesquisa</param>
        /// <returns>True se o texto contém o termo (case-insensitive e sem acentos)</returns>
        public static bool ContainsIgnoreCaseAndAccents(string text, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(searchTerm))
                return string.IsNullOrWhiteSpace(searchTerm);

            // Normalizar ambos os textos
            string normalizedText = RemoveAccents(text).ToLowerInvariant();
            string normalizedSearchTerm = RemoveAccents(searchTerm).ToLowerInvariant();

            return normalizedText.Contains(normalizedSearchTerm);
        }
    }
}
