using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using StudyMinder.Services;

namespace StudyMinder.Views
{
    public partial class AdicionarAssuntosEmLoteDialog : Window, INotifyPropertyChanged
    {
        private string _textoAssuntos = string.Empty;
        private int _totalLinhas = 0;
        private bool _temAssuntos = false;
        private ObservableCollection<string> _assuntosPreview = new();

        public AdicionarAssuntosEmLoteDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string TextoAssuntos
        {
            get => _textoAssuntos;
            set
            {
                if (SetProperty(ref _textoAssuntos, value))
                {
                    AtualizarPreview();
                }
            }
        }

        public int TotalLinhas
        {
            get => _totalLinhas;
            set => SetProperty(ref _totalLinhas, value);
        }

        public bool TemAssuntos
        {
            get => _temAssuntos;
            set => SetProperty(ref _temAssuntos, value);
        }

        public ObservableCollection<string> AssuntosPreview
        {
            get => _assuntosPreview;
            set => SetProperty(ref _assuntosPreview, value);
        }

        public List<string> AssuntosParaAdicionar { get; private set; } = new();

        private void AtualizarPreview()
        {
            var linhas = TextoAssuntos
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(linha => linha.Trim())
                .Where(linha => !string.IsNullOrWhiteSpace(linha))
                .ToList();

            TotalLinhas = linhas.Count;
            TemAssuntos = linhas.Count > 0;

            AssuntosPreview.Clear();
            foreach (var linha in linhas.Take(10)) // Mostrar apenas os primeiros 10 no preview
            {
                AssuntosPreview.Add(linha);
            }

            if (linhas.Count > 10)
            {
                AssuntosPreview.Add($"... e mais {linhas.Count - 10} assuntos");
            }

            AssuntosParaAdicionar = linhas;
        }

        private void AdicionarAssuntos_Click(object sender, RoutedEventArgs e)
        {
            if (AssuntosParaAdicionar.Count == 0)
            {
                // Usar CustomMessageBoxWindow para aviso
                NotificationService.Instance.ShowWarning(
                    "Aviso",
                    "Por favor, digite pelo menos um assunto.");
                return;
            }

            // Usar CustomMessageBoxWindow para confirmação
            var resultado = NotificationService.Instance.ShowConfirmation(
                "Confirmar Adição",
                $"Deseja adicionar {AssuntosParaAdicionar.Count} assunto(s) à disciplina?");

            if (resultado == ToastMessageBoxResult.Yes)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
