using System;

namespace StudyMinder.Utils
{
    public static class DateUtils
    {
        /// <summary>
        /// Retorna o primeiro dia da semana (domingo) para uma data específica.
        /// </summary>
        /// <param name="data">A data de referência.</param>
        /// <returns>O objeto DateTime correspondente ao domingo da semana da data de referência.</returns>
        public static DateTime GetInicioSemana(DateTime data)
        {
            // No .NET, DayOfWeek começa com Domingo = 0.
            int diff = (7 + (data.DayOfWeek - DayOfWeek.Sunday)) % 7;
            return data.AddDays(-1 * diff).Date;
        }
    }
}