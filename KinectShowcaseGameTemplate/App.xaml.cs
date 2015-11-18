using KinectShowcaseCommon.Kinect_Processing;
using KinectShowcaseCommon.ProcessHandling;
using KinectShowcaseGameTemplate.ViewModel;
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
            KinectManager.Config config = KinectManagerConfigReader.GetConfig();
            KinectManager.Default.Init(SystemCanary.Default, config);
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            string handX = "500", handY = "500";
            string trackedX = "-1", trackedY = "-1", trackedZ = "1";
            if (e.Args.Length > 0)
            {
                SystemCanary.Default.DidStartWithStreamHandle(e.Args[0]);

                if (e.Args.Length > 2)
                {
                     handX = e.Args[1];
                     handY = e.Args[2];
                }

                if (e.Args.Length > 4)
                {
                    trackedX = e.Args[3];
                    trackedY = e.Args[4];
                    trackedZ = e.Args[5];
                }
            }

            System.Threading.Timer timer = null;
            timer = new System.Threading.Timer((obj) =>
            {
                float startX = 0.5f;
                float startY = 0.5f;
                float.TryParse(handX, out startX);
                float.TryParse(handY, out startY);
                //do a few times to get over filtering
                for (int i = 0; i < 10; i++)
                    KinectManager.Default.HandManager.InjectScaledHandLocation(new Point(startX, startY));


                float favorX = 0.5f;
                float favorY = 0.5f;
                float favorZ = 0.5f;
                float.TryParse(trackedX, out favorX);
                float.TryParse(trackedY, out favorY);
                float.TryParse(trackedZ, out favorZ);
                //do a few times to get over filtering
                if (favorX != -1 && favorY != -1 && favorZ != -1)
                    KinectManager.Default.FavorNearest(favorX, favorY, favorZ);
            },
            null, 1500, System.Threading.Timeout.Infinite);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            //ViewModelLocator.Cleanup();
        }
    }
}
