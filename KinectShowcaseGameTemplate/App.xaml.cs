using KinectShowcaseCommon.Kinect_Processing;
using KinectShowcaseCommon.ProcessHandling;
using KinectShowcaseGameTemplate.ViewModel;
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace KinectShowcaseGameTemplate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public App()
            : base()
        {
            log4net.Config.XmlConfigurator.Configure();

            //init the kinect manager
            KinectManager.Config config = KinectManagerConfigReader.GetConfig();
            KinectManager.Default.Init(SystemCanary.Default, config);
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            log.Debug("starting...");
            if (e.Args.Length > 1)
            {
                Thread.Sleep(15000);
                log.Debug("Received IN: " + e.Args[0] + " OUT: " + e.Args[1]);
                SystemCanary.Default.DidStartWithStreamHandles(e.Args[0], e.Args[1]);
            }

            /*
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
            */
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            //ViewModelLocator.Cleanup();
        }
    }
}
