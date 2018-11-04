
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FlashSearch.Viewer.Services
{
    public class FileFilter
    {
        [XmlElement]
        public string Name { get; set; }

        [XmlElement]
        public string Regex { get; set; }

        public FileFilter()
        {
            Name = String.Empty;
            Regex = String.Empty;
        }

        public override string ToString() => Name;
    }
    
    public class SearchConfig
    {
        [XmlElement(ElementName = "ExcludedExtensions")]
        public string ExcludedExtensionsString { get; set; }

        [XmlArrayItem(typeof(FileFilter), ElementName = "FileFilter")]
        public List<FileFilter> FileFilters;
        
        [XmlIgnore]
        private string[] _excludedExtensions;
        
        [XmlIgnore]
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
            ExcludedExtensionsString = String.Empty;
            FileFilters = new List<FileFilter>();
            _excludedExtensions = new string[] { };
        }

        static SearchConfig Default => new SearchConfig()
        {
            ExcludedExtensionsString = ".exe, .pdb, .dll, .db, .idb, .obj, .uasset, .ipch, .cache, .zip, .rar, .7z",
            FileFilters = new List<FileFilter>()
            {
                new FileFilter() { Name = "C#", Regex = @"\.(cs)$" },
                new FileFilter() { Name = "C++", Regex = @"\.(c|cpp|h|hpp)$" },
                new FileFilter() { Name = "Data", Regex = @"\.(xml|json)$" },
            }
        };

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
                var searchConfig = SearchConfig.Default;
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