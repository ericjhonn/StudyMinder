using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using StudyMinder.Models;

namespace StudyMinder.Converters
{
    /// <summary>
    /// Conversor para calcular acertos consolidados de um edital.
    /// Recebe array com [Edital].
    /// </summary>
    public class EditalAcertosConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 1 && values[0] is Edital edital)
            {
                int totalAcertos = 0;
                
                if (edital.EditalAssuntos != null)
                {
                    foreach (var editalAssunto in edital.EditalAssuntos)
                    {
                        if (editalAssunto.Assunto?.Estudos != null)
                        {
                            totalAcertos += editalAssunto.Assunto.Estudos
                                .Where(e => e.DataTicks >= edital.DataAbertura.Ticks && e.DataTicks <= edital.DataProva.Ticks)
                                .Sum(e => e.Acertos);
                        }
                    }
                }
                
                return totalAcertos.ToString();
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para calcular erros consolidados de um edital.
    /// Recebe array com [Edital].
    /// </summary>
    public class EditalErrosConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 1 && values[0] is Edital edital)
            {
                int totalErros = 0;
                
                if (edital.EditalAssuntos != null)
                {
                    foreach (var editalAssunto in edital.EditalAssuntos)
                    {
                        if (editalAssunto.Assunto?.Estudos != null)
                        {
                            totalErros += editalAssunto.Assunto.Estudos
                                .Where(e => e.DataTicks >= edital.DataAbertura.Ticks && e.DataTicks <= edital.DataProva.Ticks)
                                .Sum(e => e.Erros);
                        }
                    }
                }
                
                return totalErros.ToString();
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para calcular total de questões consolidado de um edital.
    /// Recebe array com [Edital].
    /// </summary>
    public class EditalTotalQuestoesConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 1 && values[0] is Edital edital)
            {
                int totalAcertos = 0;
                int totalErros = 0;
                
                if (edital.EditalAssuntos != null)
                {
                    foreach (var editalAssunto in edital.EditalAssuntos)
                    {
                        if (editalAssunto.Assunto?.Estudos != null)
                        {
                            var estudosFiltrados = editalAssunto.Assunto.Estudos
                                .Where(e => e.DataTicks >= edital.DataAbertura.Ticks && e.DataTicks <= edital.DataProva.Ticks)
                                .ToList();
                            
                            totalAcertos += estudosFiltrados.Sum(e => e.Acertos);
                            totalErros += estudosFiltrados.Sum(e => e.Erros);
                        }
                    }
                }
                
                return (totalAcertos + totalErros).ToString();
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para calcular rendimento consolidado de um edital.
    /// Rendimento = (Total de Acertos) / (Total de Acertos + Total de Erros) * 100
    /// Recebe array com [Edital].
    /// </summary>
    public class EditalRendimentoConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 1 && values[0] is Edital edital)
            {
                int totalAcertos = 0;
                int totalErros = 0;
                
                if (edital.EditalAssuntos != null)
                {
                    foreach (var editalAssunto in edital.EditalAssuntos)
                    {
                        if (editalAssunto.Assunto?.Estudos != null)
                        {
                            var estudosFiltrados = editalAssunto.Assunto.Estudos
                                .Where(e => e.DataTicks >= edital.DataAbertura.Ticks && e.DataTicks <= edital.DataProva.Ticks)
                                .ToList();
                            
                            totalAcertos += estudosFiltrados.Sum(e => e.Acertos);
                            totalErros += estudosFiltrados.Sum(e => e.Erros);
                        }
                    }
                }
                
                int totalQuestoes = totalAcertos + totalErros;
                double rendimento = totalQuestoes > 0 ? (double)totalAcertos / totalQuestoes * 100 : 0;
                
                return rendimento.ToString("F1") + "%";
            }
            return "0.0%";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para calcular horas estudadas consolidadas de um edital.
    /// Recebe array com [Edital].
    /// </summary>
    public class EditalHorasEstudadasConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 1 && values[0] is Edital edital)
            {
                double totalHoras = 0;
                
                if (edital.EditalAssuntos != null)
                {
                    foreach (var editalAssunto in edital.EditalAssuntos)
                    {
                        if (editalAssunto.Assunto?.Estudos != null)
                        {
                            totalHoras += editalAssunto.Assunto.Estudos
                                .Where(e => e.DataTicks >= edital.DataAbertura.Ticks && e.DataTicks <= edital.DataProva.Ticks)
                                .Sum(e => TimeSpan.FromTicks(e.DuracaoTicks).TotalHours);
                        }
                    }
                }
                
                // Converter horas decimais para o formato HHhMM
                int horas = (int)Math.Floor(totalHoras);
                int minutos = (int)Math.Round((totalHoras - horas) * 60);
                
                // Ajustar caso os minutos arredondem para 60
                if (minutos >= 60)
                {
                    horas += 1;
                    minutos = 0;
                }
                
                return $"{horas}h{minutos:00}";
            }
            return "0h00";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para calcular total de páginas lidas consolidado de um edital.
    /// Recebe array com [Edital].
    /// </summary>
    public class EditalTotalPaginasLidasConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 1 && values[0] is Edital edital)
            {
                int totalPaginas = 0;
                
                if (edital.EditalAssuntos != null)
                {
                    foreach (var editalAssunto in edital.EditalAssuntos)
                    {
                        if (editalAssunto.Assunto?.Estudos != null)
                        {
                            totalPaginas += editalAssunto.Assunto.Estudos
                                .Where(e => e.DataTicks >= edital.DataAbertura.Ticks && e.DataTicks <= edital.DataProva.Ticks)
                                .Sum(e => e.TotalPaginas);
                        }
                    }
                }
                
                return totalPaginas.ToString();
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
