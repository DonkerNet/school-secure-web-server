using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SecureWebServer.Service.Config
{
    public class ServerConfiguration
    {
        private static ServerConfiguration _instance;

        private const string FilePath = "ServerConfiguration.json";
        public static Action<ServerConfiguration> SavedCallback { get; set; }

        public int WebPort { get; set; }
        public string WebRoot { get; set; }
        public IList<string> DefaultPages { get; set; }
        public bool DirectoryBrowsing { get; set; }

        private ServerConfiguration()
        {
        }

        /// <summary>
        /// Gets the current configuration instance or loads it from the file.
        /// </summary>
        public static ServerConfiguration Get()
        {
            if (_instance == null)
            {
                // If no instance exists yet, load it from the file

                ServerConfiguration config = null;

                if (File.Exists(FilePath))
                {
                    string json;

                    using (StreamReader reader = new StreamReader(FilePath))
                        json = reader.ReadToEnd();

                    try
                    {
                        config = JsonConvert.DeserializeObject<ServerConfiguration>(json);
                    }
                    catch
                    {
                        // Ignore invalid JSON
                    }
                }

                _instance = config ?? new ServerConfiguration();
            }

            // Return a copy so that any changes to that copy do not affect configuration instances used by other threads (requests)
            return _instance.Copy();
        }

        /// <summary>
        /// Updates this configuration instance with the specified values.
        /// </summary>
        public void SetValues(NameValueCollection values)
        {
            DirectoryBrowsing = false;

            foreach (string key in values.Keys)
            {
                switch (key)
                {
                    case nameof(WebPort):
                        int webPort;
                        if (int.TryParse(values[key], out webPort))
                            WebPort = webPort;
                        break;
                    case nameof(WebRoot):
                        WebRoot = values[key];
                        break;
                    case nameof(DefaultPages):
                        string defaultPages = values[key];
                        if (defaultPages != null)
                            DefaultPages = defaultPages.Split(';').ToList();
                        break;
                    case nameof(DirectoryBrowsing):
                        DirectoryBrowsing = string.Equals(values[key], "true", StringComparison.InvariantCultureIgnoreCase);
                        break;
                }
            }
        }

        /// <summary>
        /// Saves this configuration's changes and writes these to the file.
        /// </summary>
        public void Save()
        {
            ServerConfiguration copy = Copy();

            string json = JsonConvert.SerializeObject(copy, Formatting.Indented);

            using (StreamWriter writer = new StreamWriter(FilePath, false))
            {
                writer.Write(json);
                writer.Flush();
            }

            _instance = copy;

            var callBack = SavedCallback;
            callBack?.Invoke(this);
        }

        /// <summary>
        /// Creates a copy of this configuration instance.
        /// </summary>
        public ServerConfiguration Copy()
        {
            return new ServerConfiguration
            {
                WebPort = WebPort,
                WebRoot = WebRoot,
                DefaultPages = new List<string>(DefaultPages),
                DirectoryBrowsing = DirectoryBrowsing
            };
        }
    }
}