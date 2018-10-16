using Autofac;
using FlashSearch.Viewer.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

namespace FlashSearch.Viewer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _rightContent;

        public ViewModelBase RightContent
        {
            get => _rightContent;
            set => Set(ref _rightContent, value);
        }

        public MainViewModel()
        {
            RightContent = GlobalFactory.Container.Resolve<WelcomeViewModel>();
            Messenger.Default.Register<SearchEvent>(this, SelectedSearchResultChanged);
        }

        private void SelectedSearchResultChanged(SearchEvent result)
        {
            RightContent?.Cleanup();
            if (result == null)
            {
                RightContent = GlobalFactory.Container.Resolve<WelcomeViewModel>();
            }
            else
            {
                RightContent = GlobalFactory.Container.Resolve<ResultPeekViewModel>(
                    new NamedParameter("searchResult", result.SelectedResult),
                    new NamedParameter("contentSelector", result.ContentSelector));
            }
        }
    }
}