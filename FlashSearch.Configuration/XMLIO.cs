using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace FlashSearch.Configuration
{
    public class XMLIO
    {
        public static T Load<T>(string path)
        {
            // If the config path doesn't exist, then we return an instance of the default config.
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Could not find file.", path);
            }
            
            // Else, we parse the existing file.
            using (var reader = new StreamReader(path))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                T content = (T) xmlSerializer.Deserialize(reader);
                return content;
            }
        }

        public static void Save<T>(string path, T content)
        {
            using (var writer = new StreamWriter(path))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                xmlSerializer.Serialize(writer, content);
            }
        }
    }
}