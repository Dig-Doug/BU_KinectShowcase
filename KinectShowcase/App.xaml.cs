using KinectShowcase.Models.Gallery;
using KinectShowcase.Models.Games;
using KinectShowcase.ViewModel;
using KinectShowcaseCommon.Kinect_Processing;
using KinectShowcaseCommon.ProcessHandling;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KinectShowcase
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string DATA_DIR_NAME = "KinectShowcase";
        private const string CONFIG_FILE = "config.json";
        private const string DEFAULT_CONFIG_FILE = "default_config.json";

        private string dataDir;
        private KinectShowcaseConfig config;

        public App()
            : base()
        {
            log4net.Config.XmlConfigurator.Configure();

            this.InitConfig();

            log.Info("Getting kinect manager config");

            log.Info("Initializing kinect");
            //init the kinect manager
            KinectManager.Default.Init(SystemWatchdog.Default, config.KinectManagerConfig);

            GamesDatabase.Default.GamesRootFolder = this.dataDir + System.IO.Path.DirectorySeparatorChar + this.config.GamesDir;
            GalleryItemManager.Default.FolderIconPath = "pack://application:,,,/KinectShowcase;component/Assets/Buttons/folder_icon.png";
            GalleryItemManager.Default.RootFolder = this.dataDir + System.IO.Path.DirectorySeparatorChar + this.config.GalleryDir;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            //Log.Info("Hello World");
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            ViewModelLocator.Cleanup();
            SystemWatchdog.Default.OnExit();
        }

        private void InitConfig()
        {
            // Get the root data directory path
            string path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                path = Directory.GetParent(path).ToString();
            }

            // Create the data dir if it doesn't exist
            this.dataDir = path + System.IO.Path.DirectorySeparatorChar + DATA_DIR_NAME;
            Directory.CreateDirectory(this.dataDir);

            // Init with default config
            this.config = new KinectShowcaseConfig();

            // Write a default config file for reference
            string defConfigFilePath = this.dataDir + System.IO.Path.DirectorySeparatorChar + DEFAULT_CONFIG_FILE;
            File.WriteAllText(defConfigFilePath, JsonConvert.SerializeObject(this.config, Formatting.Indented));

            // Check if the config file exists
            string configFilePath = this.dataDir + System.IO.Path.DirectorySeparatorChar + CONFIG_FILE;
            if (File.Exists(configFilePath))
            {
                try
                {
                    // Read and parse the config file
                    string configData = File.ReadAllText(configFilePath);
                    this.config = JsonConvert.DeserializeObject<KinectShowcaseConfig>(configData);
                }
                catch (JsonException e)
                {
                    // TODO(doug) - Write error to log
                }
            }

            // Create directories if they don't exist
            Directory.CreateDirectory(this.dataDir + System.IO.Path.DirectorySeparatorChar + this.config.GamesDir);
            Directory.CreateDirectory(this.dataDir + System.IO.Path.DirectorySeparatorChar + this.config.GalleryDir);
        }
    }
}
