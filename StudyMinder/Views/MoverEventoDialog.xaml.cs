using System;
using System.Windows;

namespace StudyMinder.Views
{
    public partial class MoverEventoDialog : Window
    {
        public DateTime DataSelecionada { get; private set; }

        public MoverEventoDialog(DateTime dataAtual)
        {
            InitializeComponent();
            DtNovaData.SelectedDate = dataAtual;
            DtNovaData.DisplayDate = dataAtual; // Foca o calendário na data atual
            DataSelecionada = dataAtual;
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (DtNovaData.SelectedDate.HasValue)
            {
                DataSelecionada = DtNovaData.SelectedDate.Value;
                DialogResult = true;
            }
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}