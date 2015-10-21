using KinectShowcaseCommon.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectShowcase.Models.Games
{
    public class GameDataModel
    {
        public string RootPath { get; private set; }
        public string ExecutablePath { get; private set; }
        public string Title { get; private set; } 
        public string IconPath { get; private set; }
        public string ScreenshotPath { get; private set; }
        public string DescriptionPath { get; private set; }

        public GameDataModel(string aRootPath, string aExePath, string aTitle, string aIconPath, string aScreenshotPath, string aDescriptionPath)
        {
            this.RootPath = aRootPath;
            this.ExecutablePath = aExePath;
            this.Title = aTitle;
            this.IconPath = aIconPath;
            this.ScreenshotPath = aScreenshotPath;
            this.DescriptionPath = aDescriptionPath;
        }
    }
}
