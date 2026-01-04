using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StudyMinder.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = string.Empty;

        [RelayCommand]
        private void Refresh()
        {
            OnRefresh();
        }

        protected virtual void OnRefresh()
        {
        }

        protected virtual void OnLoading()
        {
        }

        protected virtual void OnUnloading()
        {
        }
    }
}
