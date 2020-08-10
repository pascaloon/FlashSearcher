using System;
using System.IO;
using System.Reflection;

namespace FlashSearch.Configuration
{
    public class ConfigurationPathResolver
    {
        private readonly String ConfigFileName = "SearchConfiguration.xml";
        private string ConfigFilePath = null;
        private string IndexesDir = null;

        public string GetConfigurationPath()
        {
            ResolvePathsIfEmpty();
            return ConfigFilePath;
        }

        public string GetIndexDir(string projectName, string fileFilterIndex)
        {
            ResolvePathsIfEmpty();
            return Path.Combine(IndexesDir, projectName, fileFilterIndex);
        }

        private void ResolvePathsIfEmpty()
        {
            if (String.IsNullOrEmpty(ConfigFilePath))
            {
                ConfigFilePath = ResolveConfigurationPath();
                IndexesDir = ResolveIndexesDir(ConfigFilePath);
            }
        }

        public string ResolveConfigurationPath()
        {
            string path = GetPathFromAssembly();
            if (File.Exists(path))
                return path;

            return GetPathFromLocalAppData();
        }

        private string GetPathFromAssembly()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (String.IsNullOrEmpty(directoryName))
                throw new Exception($"Unable to find executable's directory");

            return Path.Combine(directoryName, ConfigFileName);
        }

        private string GetPathFromLocalAppData()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists(localAppDataPath))
                throw new Exception($"Unable to find local app data directory '{localAppDataPath}'.");

            string flashSearcherDataDir = Path.Combine(localAppDataPath, "FlashSearcher");
            if (!Directory.Exists(flashSearcherDataDir))
                Directory.CreateDirectory(flashSearcherDataDir);

            return Path.Combine(flashSearcherDataDir, ConfigFileName);
        }

        private string ResolveIndexesDir(string configFilePath)
        {
            string configFileDir = Path.GetDirectoryName(configFilePath);
            return Path.Combine(configFileDir, "Indexes");
        }
    }
}
