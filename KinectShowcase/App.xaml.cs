using KinectShowcase.Models.Gallery;
using KinectShowcase.Models.Games;
using KinectShowcaseCommon.Kinect_Processing;
using KinectShowcaseCommon.ProcessHandling;
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
        private const string CONFIG_FILE = "showcase_config.txt";
        private const string CONFIG_SKIP = "#";
        private const string CONFIG_GAMES_PATH = "game=";
        private const string CONFIG_PICS_PATH = "pics=";
        private const string CONFIG_FOLDER_ICON = "fldi=";

        private string _gameDirectory;
        private string _pictureDirectory;
        private string _folderIconPath;

        public App()
            : base()
        {
            this.GetConfig();

            //init the kinect manager
            KinectManager.Default.Init(SystemWatchdog.Default);

            if (_gameDirectory != null && Directory.Exists(_gameDirectory))
            {
                GamesDatabase.Default.GamesRootFolder = this._gameDirectory;   
            }
            if (_pictureDirectory != null && Directory.Exists(_pictureDirectory))
            {
                GalleryItemManager.Default.FolderIconPath = _folderIconPath;
                GalleryItemManager.Default.RootFolder = _pictureDirectory;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            SystemWatchdog.Default.OnExit();
        }

        private void GetConfig()
        {
            try
            {
                /*
                string fullName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                int index = 0;
                while (exeName.IndexOf("\\") != -1)
                {
                    index = exeName.IndexOf("\\");
                    exeName = exeName.Substring(index + 1);
                }
                string manifestPath = fullName.Substring(0, fullName.IndexOf(exeName)) + CONFIG_FILE;
                */
                string manifestPath = "C:\\KinectShowcase\\showcase_config.txt";
                if (File.Exists(manifestPath))
                {
                    using (StreamReader manifestStream = new StreamReader(manifestPath))
                    {
                        int lineCount = 0;
                        while (!manifestStream.EndOfStream)
                        {
                            lineCount++;

                            string line = manifestStream.ReadLine();

                            //check if this line is a comment
                            if (!line.Substring(0, 1).Equals(CONFIG_SKIP))
                            {
                                if (line.Length > 5)
                                {
                                    //get the line prefix
                                    string prefix = line.Substring(0, 5);
                                    //get the data after it
                                    string data = line.Substring(5, line.Length - 5);

                                    //process the prefix & data
                                    if (prefix.Equals(CONFIG_GAMES_PATH))
                                    {
                                        this._gameDirectory = data;
                                    }
                                    else if (prefix.Equals(CONFIG_PICS_PATH))
                                    {
                                        _pictureDirectory = data;
                                    }
                                    else if (prefix.Equals(CONFIG_FOLDER_ICON))
                                    {
                                        _folderIconPath = data;
                                    }
                                    else
                                    {
                                        //unknown tag
                                        Debug.WriteLine("App - WARN - Line " + lineCount + "had an unknown prefix");
                                    }
                                }
                                else
                                {
                                    //line wasn't long enough to be valid
                                    Debug.WriteLine("App - WARN - Line " + lineCount + "was too short be valid");
                                }
                            }
                        }

                        manifestStream.Close();
                    }
                }
            }
            catch (IOException e)
            {
                //error
                Debug.WriteLine("App - EXCEPTION - Exception Message: " + e.Message);
            }
        }
    }
}
