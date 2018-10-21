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

            // Register File Service
            builder.RegisterType<FileService>().AsSelf().SingleInstance();
            
            // Register Search Config
            builder.RegisterInstance(SearchConfig.Load()).AsSelf().SingleInstance();
            
            // Register View Models
            ViewModelLocator.RegisterTypes(builder);

            Container = builder.Build();
        }
    }
}