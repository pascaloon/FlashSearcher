using System.Reflection;
using Autofac;
using FlashSearch.Viewer.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;

namespace FlashSearch.Viewer.ViewModels
{
    public class ViewModelLocator
    {
        public MainViewModel MainViewModel => GlobalFactory.Container.Resolve<MainViewModel>();
        public SearchViewModel SearchViewModel => GlobalFactory.Container.Resolve<SearchViewModel>();
        
        public static void RegisterTypes(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(t => t.Name.EndsWith("ViewModel"));
        }
    }
}