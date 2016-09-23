using CommandLine;
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
