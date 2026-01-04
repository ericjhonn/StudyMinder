using System.Windows;
using System.Windows.Controls;
using StudyMinder.Models;

namespace StudyMinder.Views
{
    public class AgendaItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? EstudoTemplate { get; set; }
        public DataTemplate? RevisaoTemplate { get; set; }
        public DataTemplate? EditalTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                Estudo _ => EstudoTemplate,
                Revisao _ => RevisaoTemplate,
                EditalCronograma _ => EditalTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}
