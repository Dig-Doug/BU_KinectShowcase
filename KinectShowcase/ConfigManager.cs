using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcase
{
    class ConfigManager
    {
        private const string DATA_DIR_NAME = "KinectShowcase";
        private const string CONFIG_FILE = "config.json";

        public KinectShowcaseConfig getConfig()
        {
            // Get the root data directory path
            string path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                path = Directory.GetParent(path).ToString();
            }

            // Create the data dir if it doesn't exist
            string dataDirPath = path + System.IO.Path.DirectorySeparatorChar + DATA_DIR_NAME;
            Directory.CreateDirectory(dataDirPath);

            // Init with default config
            KinectShowcaseConfig config = new KinectShowcaseConfig();
            // Check if the config file exists
            string configFilePath = dataDirPath + System.IO.Path.DirectorySeparatorChar + CONFIG_FILE;
            if (File.Exists(configFilePath))
            {
                try {
                    // Read and parse the config file
                    string configData = File.ReadAllText(configFilePath);
                    config = JsonConvert.DeserializeObject<KinectShowcaseConfig>(configData);
                } catch (JsonException e)
                {
                    // TODO(doug) - Write error to log
                }
            }

            return config;
        }
    }
}
