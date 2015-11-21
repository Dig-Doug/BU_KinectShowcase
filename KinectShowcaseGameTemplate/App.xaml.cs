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
                SystemCanary.Default.DidStartWithStreamHandles(e.Args[0], e.Args[1]);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            //ViewModelLocator.Cleanup();
        }
    }
}
