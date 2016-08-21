using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcase.Models.Games
{
    public class GamesDatabase
    {
        #region Singleton
        private static volatile GamesDatabase instance;
        private static object syncRoot = new Object();
        public static GamesDatabase Default
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new GamesDatabase();
                    }
                }

                return instance;
            }
        }
        #endregion

        private const string CONFIG_FILE = "config.json";
        private const string DEFAULT_CONFIG_FILE = "default_config.json";

        private string _gamesRootFolder;
        public List<GameDataModel> Games { get; private set; }

        private GamesDatabase()
        {
            this.Games = new List<GameDataModel>();
        }

        public string GamesRootFolder
        {
            get
            {
                return _gamesRootFolder;
            }
            set
            {
                if (_gamesRootFolder != value)
                {
                    _gamesRootFolder = value;
                    this.Refresh();
                }
            }
        }

        public void Refresh()
        {
            this.Games.Clear();

            //get all sub directories
            string[] subFolderPaths = Directory.GetDirectories(_gamesRootFolder);

            //process all sub directories, see if they hold valid & complete game data
            foreach (string currentDirPath in subFolderPaths)
            {
                GameDataModel currentData = GameDataInFolder(currentDirPath);
                //check if it is a valid game data file
                if (currentData != null)
                {
                    //add it to the list
                    this.Games.Add(currentData);
                }
            }
        }

        private GameDataModel GameDataInFolder(string aFolder)
        {
            GameDataModel result = null;

            GameConfig config = new GameConfig();

            // Always write a default config file
            string defConfigFilePath = aFolder + System.IO.Path.DirectorySeparatorChar + DEFAULT_CONFIG_FILE;
            File.WriteAllText(defConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));

            // Try and get the game config
            string configPath = aFolder + "/" + CONFIG_FILE;
            if (File.Exists(configPath))
            {
                try
                {
                    config = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText(configPath));

                    if (config.HasRequiredFields())
                    {
                        result = new GameDataModel(aFolder, config.Executable, config.Title, config.Icon, config.Screenshot, config.DescriptionFile);
                    }
                }
                catch (JsonException e)
                {
                    // TODO(doug) - handle
                }
            }

            return result;
        }
    }
}
