using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StudyMinder.Controls
{
    public partial class KPICard : UserControl
    {
        public KPICard()
        {
            InitializeComponent();
        }

        // Propriedade: Título do Card
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(KPICard),
                new PropertyMetadata(string.Empty));

        // Propriedade: Valor exibido
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(KPICard),
                new PropertyMetadata(string.Empty));

        // Propriedade: Cor de fundo do ícone
        public Brush ColorBrush
        {
            get { return (Brush)GetValue(ColorBrushProperty); }
            set { SetValue(ColorBrushProperty, value); }
        }

        public static readonly DependencyProperty ColorBrushProperty =
            DependencyProperty.Register(
                nameof(ColorBrush),
                typeof(Brush),
                typeof(KPICard),
                new PropertyMetadata(null));

        // Propriedade: Tipo de ícone (PackIconMaterial Kind)
        public string IconKind
        {
            get { return (string)GetValue(IconKindProperty); }
            set { SetValue(IconKindProperty, value); }
        }

        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register(
                nameof(IconKind),
                typeof(string),
                typeof(KPICard),
                new PropertyMetadata("HelpCircle"));

        // Propriedade: Style do ícone (alternativa ao IconKind)
        public Style IconStyle
        {
            get { return (Style)GetValue(IconStyleProperty); }
            set { SetValue(IconStyleProperty, value); }
        }

        public static readonly DependencyProperty IconStyleProperty =
            DependencyProperty.Register(
                nameof(IconStyle),
                typeof(Style),
                typeof(KPICard),
                new PropertyMetadata(null));
    }
}
