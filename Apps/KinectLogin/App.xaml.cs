using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Kinect;
using Microsoft.Kinect.Wpf.Controls;
using KinectShowcaseCommon.Kinect_Processing;
using CommandLine;
using KinectShowcaseCommon.ProcessHandling;
using log4net;

namespace KinectLogin
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal KinectRegion KinectRegion { get; set; }

        class Options
        {
            [Option("master_port", DefaultValue = SystemWatchdog.MASTER_PORT, HelpText = "Port master is listening on")]
            public int MasterPort { get; set; }
            [Option("slave_port", DefaultValue = SystemWatchdog.SLAVE_PORT, HelpText = "Port slave should listen on")]
            public int SlavePort { get; set; }
        }

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public App()
            : base()
        {
            log4net.Config.XmlConfigurator.Configure();

            // TODO(doug) - Read config from file
            //init the kinect manager
            KinectManager.Config config = new KinectManager.Config();
            KinectManager.Default.Init(SystemCanary.Default, config);
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            log.Debug("starting...");
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(e.Args, options))
            {
                SystemCanary.Default.StartGRPC(options.MasterPort, options.SlavePort);
            }
            else
            {
                // TODO(doug) - Print usage
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            //ViewModelLocator.Cleanup();
        }
    }
}
