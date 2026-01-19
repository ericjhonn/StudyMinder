using System.Windows;
using MahApps.Metro.IconPacks;

namespace StudyMinder.Behaviors
{
    public static class IconHelper
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached(
                "Icon",
                typeof(PackIconMaterialKind),
                typeof(IconHelper),
                new PropertyMetadata(PackIconMaterialKind.None));

        public static void SetIcon(DependencyObject element, PackIconMaterialKind value)
            => element.SetValue(IconProperty, value);

        public static PackIconMaterialKind GetIcon(DependencyObject element)
            => (PackIconMaterialKind)element.GetValue(IconProperty);
    }
}