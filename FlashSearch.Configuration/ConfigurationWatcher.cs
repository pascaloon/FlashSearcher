using System;
using System.Diagnostics;
using System.IO;

namespace FlashSearch.Configuration
{
    
    public class ConfigurationWatcher<T>
    {
        public delegate void ConfigurationUpdatedEventHandler(object sender, T newContent);  

        public event ConfigurationUpdatedEventHandler ConfigurationUpdated = delegate(object sender, T content) {  };
        
        private readonly string _path;
        private readonly Func<string, T> _load;
        private readonly Action<string, T> _save;
        private readonly Func<T> _default;

        private T _configuration;
        private FileSystemWatcher _fileWatcher;

        public ConfigurationWatcher(string path, Func<string, T> load, Action<string, T> save, Func<T> @default)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _load = load ?? throw new ArgumentNullException(nameof(load));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _default = @default ?? throw new ArgumentNullException(nameof(@default));
            
            _configuration = default(T);

            _path = SymLinksUtils.GetFinalPathName(_path);
            _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(_path) ?? throw new ArgumentException(nameof(path)));
            _fileWatcher.Filter = Path.GetFileName(path);
            _fileWatcher.Changed += FileWatcherOnChanged;
            _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _fileWatcher.EnableRaisingEvents = true;
            _fileWatcher.IncludeSubdirectories = true;

        }

        public T GetConfiguration()
        {
            if (_configuration == null)
                LoadConfiguration();
            return _configuration;
        }

        public void UpdateConfiguration(T newConfiguration)
        {
            _configuration = newConfiguration;
            SaveConfiguration();
        }

        private void LoadConfiguration()
        {
            if (File.Exists(_path))
            {
                T newConfig = _load(_path);
                if (newConfig != null)
                    _configuration = newConfig;
            }
            else
            {
                _configuration = _default();
                SaveConfiguration();
            }
            
        }

        private void SaveConfiguration()
        {
            _fileWatcher.EnableRaisingEvents = false;
            _save(_path, _configuration);
            _fileWatcher.EnableRaisingEvents = true;
        }
        
        private void FileWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (!e.FullPath.Equals(_path))
                return;
            while (true)
            {
                try
                {
                    if (File.ReadAllBytes(_path).Length == 0)
                        return;
                    break;
                }
                catch (IOException)
                {
                    // Another process is still writing to the file.
                }
            }
            
            try
            {
                LoadConfiguration();
                NotifyConfigurationUpdated();
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Exception while hot reloading configuration file: {exception.Message}");
            }
        }

        private void NotifyConfigurationUpdated()
        {
            ConfigurationUpdated(this, _configuration);
        }
    }
}