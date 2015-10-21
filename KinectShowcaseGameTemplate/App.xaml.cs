using KinectShowcaseCommon.Kinect_Processing;
using KinectShowcaseCommon.ProcessHandling;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KinectShowcaseGameTemplate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public App()
            : base()
        {
            //init the kinect manager
            KinectManager.Default.Init(SystemCanary.Default);
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                SystemCanary.Default.DidStartWithStreamHandle(e.Args[0]);
            }
        }
    }
}
