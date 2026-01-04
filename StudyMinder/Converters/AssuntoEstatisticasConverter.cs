using System;
using System.Globalization;
using System.Windows.Data;
using StudyMinder.ViewModels;

namespace StudyMinder.Converters
{
    /// <summary>
    /// Conversor para calcular acertos de um assunto dentro do período do edital.
    /// Recebe array com [ViewModel, AssuntoVinculado].
    /// </summary>
    public class AssuntoAcertosConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is EditarEditalViewModel viewModel && values[1] is AssuntoVinculado assuntoVinculado)
            {
                if (viewModel.DataAbertura.HasValue && viewModel.DataProva.HasValue)
                {
                    return assuntoVinculado.GetAcertosPorPeriodo(viewModel.DataAbertura.Value, viewModel.DataProva.Value).ToString();
                }
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para calcular erros de um assunto dentro do período do edital.
    /// Recebe array com [ViewModel, AssuntoVinculado].
    /// </summary>
    public class AssuntoErrosConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is EditarEditalViewModel viewModel && values[1] is AssuntoVinculado assuntoVinculado)
            {
                if (viewModel.DataAbertura.HasValue && viewModel.DataProva.HasValue)
                {
                    return assuntoVinculado.GetErrosPorPeriodo(viewModel.DataAbertura.Value, viewModel.DataProva.Value).ToString();
                }
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para calcular total de questões de um assunto dentro do período do edital.
    /// Recebe array com [ViewModel, AssuntoVinculado].
    /// </summary>
    public class AssuntoTotalQuestoesConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is EditarEditalViewModel viewModel && values[1] is AssuntoVinculado assuntoVinculado)
            {
                if (viewModel.DataAbertura.HasValue && viewModel.DataProva.HasValue)
                {
                    return assuntoVinculado.GetTotalQuestoesPorPeriodo(viewModel.DataAbertura.Value, viewModel.DataProva.Value).ToString();
                }
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para calcular rendimento de um assunto dentro do período do edital.
    /// Recebe array com [ViewModel, AssuntoVinculado].
    /// </summary>
    public class AssuntoRendimentoConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is EditarEditalViewModel viewModel && values[1] is AssuntoVinculado assuntoVinculado)
            {
                if (viewModel.DataAbertura.HasValue && viewModel.DataProva.HasValue)
                {
                    return assuntoVinculado.GetRendimentoPorPeriodo(viewModel.DataAbertura.Value, viewModel.DataProva.Value).ToString("F1") + "%";
                }
            }
            return "0.0%";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Conversor para calcular horas estudadas de um assunto dentro do período do edital.
    /// Recebe array com [ViewModel, AssuntoVinculado].
    /// </summary>
    public class AssuntoHorasEstudadasConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is EditarEditalViewModel viewModel && values[1] is AssuntoVinculado assuntoVinculado)
            {
                if (viewModel.DataAbertura.HasValue && viewModel.DataProva.HasValue)
                {
                    double totalHoras = assuntoVinculado.GetHorasEstudadasPorPeriodo(
                        viewModel.DataAbertura.Value, 
                        viewModel.DataProva.Value
                    );
                    
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
            }
            return "0h00";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
