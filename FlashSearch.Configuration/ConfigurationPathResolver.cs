using System;
using System.IO;
using System.Reflection;

namespace FlashSearch.Configuration
{
    public static class ConfigurationPathResolver
    {
        private static readonly String ConfigFileName = "SearchConfiguration.xml";
        private static string ConfigFilePath = null;
        private static string IndexesDir = null;

        public static string GetConfigurationPath()
        {
            ResolvePathsIfEmpty();
            return ConfigFilePath;
        }

        public static string GetIndexDir(string projectName, string fileFilterIndex)
        {
            ResolvePathsIfEmpty();
            return Path.Combine(IndexesDir, projectName, fileFilterIndex);
        }

        private static void ResolvePathsIfEmpty()
        {
            if (String.IsNullOrEmpty(ConfigFilePath))
            {
                ConfigFilePath = ResolveConfigurationPath();
                IndexesDir = ResolveIndexesDir(ConfigFilePath);
            }
        }

        public static string ResolveConfigurationPath()
        {
            string path = GetPathFromAssembly();
            if (File.Exists(path))
                return path;

            return GetPathFromLocalAppData();
        }

        private static string GetPathFromAssembly()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (String.IsNullOrEmpty(directoryName))
                return ConfigFileName;

            return Path.Combine(directoryName, ConfigFileName);
        }

        private static string GetPathFromLocalAppData()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists(localAppDataPath))
                throw new Exception($"Unable to find local app data directory '{localAppDataPath}'.");

            string flashSearcherDataDir = Path.Combine(localAppDataPath, "FlashSearcher");
            if (!Directory.Exists(flashSearcherDataDir))
                Directory.CreateDirectory(flashSearcherDataDir);

            return Path.Combine(flashSearcherDataDir, ConfigFileName);
        }

        private static string ResolveIndexesDir(string configFilePath)
        {
            string configFileDir = Path.GetDirectoryName(configFilePath);
            return Path.Combine(configFileDir, "Indexes");
        }
    }
}
