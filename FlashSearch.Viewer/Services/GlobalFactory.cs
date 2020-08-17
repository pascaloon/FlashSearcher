using System;
using System.IO;
using System.Reflection;
using Autofac;
using FlashSearch.Configuration;
using FlashSearch.Viewer.ViewModels;

namespace FlashSearch.Viewer.Services
{
    public class GlobalFactory
    {
        public static IContainer Container { get; }

        static GlobalFactory()
        {
            var builder = new ContainerBuilder();

            // Register File Service
            builder.RegisterType<FileService>().AsSelf().SingleInstance();

            ConfigurationPathResolver configPathResolver = new ConfigurationPathResolver();
            builder.RegisterInstance(configPathResolver).AsSelf().SingleInstance();

            string configurationPath = configPathResolver.GetConfigurationPath();

            // Register Search Configuration
            builder.RegisterInstance(
                new ConfigurationWatcher<SearchConfiguration>(
                    configurationPath, 
                    XMLIO.Load<SearchConfiguration>,
                    XMLIO.Save,
                    () => SearchConfiguration.Default))
                .AsSelf().SingleInstance();
            
            // Register View Models
            ViewModelLocator.RegisterTypes(builder);

            Container = builder.Build();
        }
    }
}