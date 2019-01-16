using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace FlashSearch.Configuration
{
    public class FileFilter
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Regex { get; set; }

        [XmlAttribute]
        public string Index { get; set; }
        
        public FileFilter()
        {
            Name = String.Empty;
            Regex = String.Empty;
            Index = String.Empty;
        }

        public override string ToString() => Name;
    }

    public class Project
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Path { get; set; }

        public Project()
        {
            Name = String.Empty;
            Path = String.Empty;
        }
        
        public override string ToString() => Name;
    }
    
    public class SearchConfiguration
    {
        [XmlElement(ElementName = "ExcludedExtensions")]
        public string ExcludedExtensionsString { get; set; }

        [XmlArrayItem(typeof(FileFilter), ElementName = "FileFilter")]
        public List<FileFilter> FileFilters;
        
        [XmlArrayItem(typeof(Project), ElementName = "Projects")]
        public List<Project> Projects;
        
        [XmlArrayItem(typeof(string), ElementName = "Path")]
        public List<string> ExcludedPaths;
        
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


        public SearchConfiguration()
        {
            ExcludedExtensionsString = String.Empty;
            FileFilters = new List<FileFilter>();
            _excludedExtensions = new string[] { };
            ExcludedPaths = new List<string>();
        }

        public static SearchConfiguration Default => new SearchConfiguration()
        {
            ExcludedExtensionsString = ".exe, .pdb, .dll, .db, .idb, .obj, .uasset, .ipch, .cache, .zip, .rar, .7z",
            FileFilters = new List<FileFilter>()
            {
                new FileFilter() { Name = "C#", Regex = @"\.(cs)$", Index = "Cs"},
                new FileFilter() { Name = "C++", Regex = @"\.(c|cpp|h|hpp)$", Index = "Cpp" },
                new FileFilter() { Name = "Data", Regex = @"\.(xml|json)$", Index = "Data" },
            },
            Projects = new List<Project>()
            {
                new Project() { Name = "FlashSearch", Path = @"D:\Repository\FlashSearch" }
            },
            ExcludedPaths = new List<string>() {" "}
        };
    }
}