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
            
            // Register Search Configuration
            builder.RegisterInstance(
                new ConfigurationWatcher<SearchConfiguration>(
                    GetConfigurationPath(), 
                    XMLIO.Load<SearchConfiguration>,
                    XMLIO.Save,
                    () => SearchConfiguration.Default))
                .AsSelf().SingleInstance();
            
            // Register View Models
            ViewModelLocator.RegisterTypes(builder);

            Container = builder.Build();
        }

        static string GetConfigurationPath()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (String.IsNullOrEmpty(directoryName))
                throw new Exception("Unable to find executable's directory.");
            return Path.Combine(directoryName, "SearchConfiguration.xml");
        }
    }
}