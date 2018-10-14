using Autofac;
using FlashSearch.Viewer.ViewModels;

namespace FlashSearch.Viewer.Services
{
    public class GlobalFactory
    {
        public static IContainer Container { get; set; }

        static GlobalFactory()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<FileService>().AsSelf().SingleInstance();
            
            ViewModelLocator.RegisterTypes(builder);

            Container = builder.Build();
        }
    }
}