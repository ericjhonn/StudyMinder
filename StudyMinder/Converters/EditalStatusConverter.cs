using StudyMinder.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace StudyMinder.Converters
{
    public class EditalStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Edital edital)
                return "Em andamento";

            // NOVO: Se boleto não está pago e já é a data da prova, retorna "Não Realizado"
            if (!edital.BoletoPago && DateTime.Now.Date >= edital.DataProva.Date)
                return "Não Realizado";

            // Se o edital não está encerrado (sem homologação), sempre retorna "Em andamento"
            if (!edital.Encerrado)
                return "Em andamento";

            // Se não houver informações de vagas/colocação mesmo após encerramento
            // Também considera Colocacao = 0 como inválido (não preenchido)
            if (!edital.VagasImediatas.HasValue || !edital.Colocacao.HasValue || edital.Colocacao.Value == 0)
                return "Em andamento";

            var colocacao = edital.Colocacao.Value;
            var vagasImediatas = edital.VagasImediatas.Value;
            var vagasCadastroReserva = edital.VagasCadastroReserva ?? 0;

            // Aprovado: Colocação <= VagasImediatas
            if (colocacao <= vagasImediatas)
                return "Aprovado";

            // Classificado: Colocação > VagasImediatas E Colocação <= VagasCadastroReserva
            if (colocacao <= vagasCadastroReserva)
                return "Classificado";

            // Eliminado: Colocação > VagasCadastroReserva
            return "Eliminado";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Conversor que retorna um Brush dinâmico baseado no status do edital
    /// Usa as cores do tema atual (Light/Dark) automaticamente
    /// </summary>
    public class EditalStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Edital edital)
                return GetBrushFromResource("WarningBrush"); // Em andamento

            // NOVO: Se boleto não está pago e já é a data da prova, retorna vermelho escuro
            if (!edital.BoletoPago && DateTime.Now.Date >= edital.DataProva.Date)
                return GetBrushFromResource("BorderBrush"); // Não Realizado (Vermelho)

            // Se o edital não está encerrado (sem homologação), sempre retorna laranja
            if (!edital.Encerrado)
                return GetBrushFromResource("WarningBrush"); // Em andamento

            // Se não houver informações de vagas/colocação mesmo após encerramento
            // Também considera Colocacao = 0 como inválido (não preenchido)
            if (!edital.VagasImediatas.HasValue || !edital.Colocacao.HasValue || edital.Colocacao.Value == 0)
                return GetBrushFromResource("WarningBrush"); // Em andamento

            var colocacao = edital.Colocacao.Value;
            var vagasImediatas = edital.VagasImediatas.Value;
            var vagasCadastroReserva = edital.VagasCadastroReserva ?? 0;

            // Aprovado: Verde (SuccessBrush)
            if (colocacao <= vagasImediatas)
                return GetBrushFromResource("SuccessBrush");

            // Classificado: Azul (InfoBrush)
            if (colocacao <= vagasCadastroReserva)
                return GetBrushFromResource("InfoBrush");

            // Eliminado: Vermelho (ErrorBrush)
            return GetBrushFromResource("ErrorBrush");
        }

        /// <summary>
        /// Obtém um Brush do tema atual usando DynamicResource
        /// </summary>
        private static Brush GetBrushFromResource(string resourceKey)
        {
            try
            {
                if (Application.Current.Resources[resourceKey] is Brush brush)
                {
                    return brush;
                }
            }
            catch
            {
                // Se não encontrar o recurso, retorna um brush padrão
            }

            // Fallback: retorna um brush padrão
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
