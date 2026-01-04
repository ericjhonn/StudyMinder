using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StudyMinder.Models
{
    /// <summary>
    /// Representa um dia no heatmap de 31 dias
    /// </summary>
    public class HeatmapDia : INotifyPropertyChanged
    {
        private int _dia;
        private bool _temEstudo;
        private double _horasEstudadas;
        private int _totalQuestoes;

        /// <summary>
        /// Número do dia (1-31)
        /// </summary>
        public int Dia
        {
            get => _dia;
            set
            {
                if (_dia != value)
                {
                    _dia = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indica se houve estudo neste dia
        /// </summary>
        public bool TemEstudo
        {
            get => _temEstudo;
            set
            {
                if (_temEstudo != value)
                {
                    _temEstudo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Quantidade de horas estudadas neste dia
        /// </summary>
        public double HorasEstudadas
        {
            get => _horasEstudadas;
            set
            {
                if (_horasEstudadas != value)
                {
                    _horasEstudadas = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Quantidade de questões respondidas neste dia
        /// </summary>
        public int TotalQuestoes
        {
            get => _totalQuestoes;
            set
            {
                if (_totalQuestoes != value)
                {
                    _totalQuestoes = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public HeatmapDia(int dia)
        {
            _dia = dia;
            _temEstudo = false;
            _horasEstudadas = 0;
            _totalQuestoes = 0;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
