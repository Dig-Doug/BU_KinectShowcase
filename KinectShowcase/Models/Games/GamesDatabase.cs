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

        private const string MAINIFEST_FILE_NAME = "_app_manifest.txt";
        private const string MAINIFEST_LINE_SKIP = "#";
        private const string MAINIFEST_LINE_TITLE = "ttl=";
        private const string MAINIFEST_LINE_EXE = "exe=";
        private const string MAINIFEST_LINE_ICON = "icn=";
        private const string MAINIFEST_LINE_SCREENSHOT = "scr=";
        private const string MAINIFEST_LINE_DESCRIPTION = "des=";

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

            try
            {
                string manifestPath = aFolder + "/" + MAINIFEST_FILE_NAME;
                if (File.Exists(manifestPath))
                {
                    using (StreamReader manifestStream = new StreamReader(manifestPath))
                    {
                        string exePath = null, title = null, iconPath = null, screenshotPath = null, descriptionPath = null;

                        int lineCount = 0;
                        while (!manifestStream.EndOfStream)
                        {
                            lineCount++;

                            string line = manifestStream.ReadLine();

                            //check if this line is a comment
                            if (!line.Substring(0, 1).Equals(MAINIFEST_LINE_SKIP))
                            {
                                if (line.Length > 4)
                                {
                                    //get the line prefix
                                    string prefix = line.Substring(0, 4);
                                    //get the data after it
                                    string data = line.Substring(4, line.Length - 4);

                                    //process the prefix & data
                                    if (prefix.Equals(MAINIFEST_LINE_TITLE))
                                    {
                                        title = data;
                                    }
                                    else if (prefix.Equals(MAINIFEST_LINE_EXE))
                                    {
                                        exePath = data;
                                    }
                                    else if (prefix.Equals(MAINIFEST_LINE_ICON))
                                    {
                                        iconPath = data;
                                    }
                                    else if (prefix.Equals(MAINIFEST_LINE_SCREENSHOT))
                                    {
                                        screenshotPath = data;
                                    }
                                    else if (prefix.Equals(MAINIFEST_LINE_DESCRIPTION))
                                    {
                                        descriptionPath = data;
                                    }
                                    else
                                    {
                                        //unknown tag
                                        Debug.WriteLine("GameLoader - WARN - Line " + lineCount + "had an unknown prefix");
                                    }
                                }
                                else
                                {
                                    //line wasn't long enough to be valid
                                    Debug.WriteLine("GameLoader - WARN - Line " + lineCount + "was too short be valid");
                                }
                            }
                        }

                        //check that we got all the data we require
                        if (exePath != null && title != null && iconPath != null && screenshotPath != null && descriptionPath != null)
                        {
                            //WOOT! They followed all of the directions, load their game
                            result = new GameDataModel(aFolder, exePath, title, iconPath, screenshotPath, descriptionPath);
                        }
                        else
                        {
                            //figure out what's missing and print a helpful message :) How nice.
                            string missingString = "";
                            missingString += (exePath == null ? " exe" : "");
                            missingString += (title == null ? " ttl" : "");
                            missingString += (iconPath == null ? " icn" : "");
                            missingString += (screenshotPath == null ? " scr" : "");
                            missingString += (descriptionPath == null ? " des" : "");
                            Debug.WriteLine("GameLoader - ERROR - Missing required data:" + missingString);
                        }

                        manifestStream.Close();
                    }
                }
            }
            catch (IOException e)
            {
                //error
                Debug.WriteLine("GameDatabaseLoader - EXCEPTION - Exception Message: " + e.Message);
            }

            return result;
        }
    }
}
