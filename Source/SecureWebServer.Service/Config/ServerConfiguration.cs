using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PathHelper = SecureWebServer.Core.Helpers.PathHelper;

namespace SecureWebServer.Service.Config
{
    /// <summary>
    /// Contains the configuration to use for the webserver.
    /// </summary>
    public class ServerConfiguration
    {
        private static ServerConfiguration _instance;

        private const string FilePath = "ServerConfiguration.json";

        /// <summary>
        /// Gets or sets the callback to call when changes to the configuration have been made and saved to the configuration file.
        /// </summary>
        public static Action<ServerConfiguration> SavedCallback { get; set; }
        /// <summary>
        /// Gets or sets the port the webserver should use for listening.
        /// </summary>
        public int WebPort { get; set; }
        /// <summary>
        /// Gets or sets the root folder where the HTML pages and other files are stored.
        /// </summary>
        public string WebRoot { get; set; }
        /// <summary>
        /// Gets or sets a list of available default pages used for when the user navigates to the homepage.
        /// </summary>
        public IList<string> DefaultPages { get; set; }
        /// <summary>
        /// Gets or sets if directory browsing is enabled.
        /// </summary>
        public bool DirectoryBrowsing { get; set; }

        // We use a private constructor to force using the Get() method for retrieving the configuration
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

            // Return a copy so that any changes to that copy do not affect configuration instances used by other threads/requests
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
                        if (PathHelper.IsValid(values[key]))
                            WebRoot = values[key];
                        break;
                    case nameof(DefaultPages):
                        string defaultPages = values[key];
                        if (defaultPages != null)
                            DefaultPages = defaultPages.Split(';').Where(PathHelper.IsValid).ToList();
                        break;
                    case nameof(DirectoryBrowsing):
                        DirectoryBrowsing = string.Equals(values[key], "true", StringComparison.InvariantCultureIgnoreCase);
                        break;
                }
            }
        }

        /// <summary>
        /// Saves this configuration's changes, writes these to the file and calls the callback method.
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

        /// <summary>
        /// Gets the first existing default page that is found, based on the pages configured in the <see cref="DefaultPages"/> property.
        /// </summary>
        public string GetExistingDefaultPage()
        {
            foreach (string defaultPage in DefaultPages)
            {
                string path = PathHelper.Combine(WebRoot, defaultPage);
                if (File.Exists(path))
                    return defaultPage;
            }

            return string.Empty;
        }
    }
}