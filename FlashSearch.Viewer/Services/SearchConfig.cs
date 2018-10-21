
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace FlashSearch.Viewer.Services
{
    public class SearchConfig
    {
        [XmlElement(ElementName = "ExcludedExtensions")]
        public string ExcludedExtensionsString { get; set; }

        private string[] _excludedExtensions;
        public string[] ExcludedExtensions
        {
            get
            {
                if (!_excludedExtensions.Any() && !String.IsNullOrWhiteSpace(ExcludedExtensionsString))
                {
                    _excludedExtensions = ExcludedExtensionsString.Split(',').Select(s => s.Trim()).ToArray();
                }

                return _excludedExtensions;
            }
        }
        

        public SearchConfig()
        {
            ExcludedExtensionsString = ".exe, .pdb, .dll, .db, .idb, .obj, .uasset, .ipch, .cache, .zip, .rar, .7z";
            _excludedExtensions = new string[] { };
        }

        public void Save()
        {
            string configPath = GetConfigPath();
            using (var writer = new StreamWriter(configPath))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SearchConfig));
                xmlSerializer.Serialize(writer, this);
            }
        }


        public static string GetConfigPath()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (String.IsNullOrEmpty(directoryName))
                throw new Exception("Unable to find executable's directory.");
            return Path.Combine(directoryName, "SearchConfig.xml");
        }
        
        
        public static SearchConfig Load()
        {
            string configPath = GetConfigPath();
            
            // If the config path doesn't exist, then we return an instance of the default config.
            if (!File.Exists(configPath))
            {
                var searchConfig = new SearchConfig();
                searchConfig.Save();
                return searchConfig;
            }
            
            // Else, we parse the existing file.
            using (var reader = new StreamReader(configPath))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SearchConfig));
                SearchConfig config = (SearchConfig) xmlSerializer.Deserialize(reader);
                if (config == null)
                    return new SearchConfig();
                return config;
            }
        }

    }
}