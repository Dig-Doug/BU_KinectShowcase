using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectShowcaseCommon.ProcessHandling;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Security.Principal;
using System.Windows;
using KinectShowcaseCommon.Kinect_Processing;
using log4net;

namespace KinectShowcaseCommon.ProcessHandling
{
    // Canaries were brought into mineshafts to detect posionus gases
    // This one detects when the program is done :)
    public class SystemCanary : ISystemInteractionListener, IPCHandler.MessageReceiver
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static volatile SystemCanary _instance;
        private static object _syncRoot = new Object();

        public ISystemProgressListener ProgessListener;

        private IPCClient _client;

        public static SystemCanary Default
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                            _instance = new SystemCanary();
                    }
                }

                return _instance;
            }
        }

        private SystemCanary()
        {
            _client = new IPCClient();
        }

        ~SystemCanary()
        {
            _client.Close();
        }

        public void DidStartWithStreamHandles(string aInHandle, string aOutHandle)
        {
            _client.ConnectWithStreamHandles(aInHandle, aOutHandle);

            SystemMessage pingMes = new SystemMessage(SystemMessage.MessageType.Ack, DateTime.Now.ToString());
            _client.SendMessage(pingMes);
        }

        public void SystemDidRecieveInteraction()
        {
            SystemMessage interactMes = new SystemMessage(SystemMessage.MessageType.Interaction, DateTime.Now.ToString());
            _client.SendMessage(interactMes);
        }

        public void AskForKill()
        {
            if (_client.CanSend())
            {
                SystemMessage killMe = new SystemMessage(SystemMessage.MessageType.Kill, DateTime.Now.ToString());
                _client.SendMessage(killMe);
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        public void ReceivedMessage(SystemMessage aMessage)
        {
            switch (aMessage.Type)
            {
                case SystemMessage.MessageType.Ping:
                    {
                        Debug.WriteLine("SystemCanary - LOG - received ping from client");
                        break;
                    }

                case SystemMessage.MessageType.SyncHand:
                    {
                        string[] point = aMessage.Data.Split(' ');
                        if (point.Length >= 2)
                        {
                            float x = float.Parse(point[0]);
                            float y = float.Parse(point[1]);
                            for (int i = 0; i < 10; i++)
                                KinectManager.Default.HandManager.InjectScaledHandLocation(new Point(x, y));
                        }
                        else
                        {
                            log.Error("Invalid hand sync point");
                        }

                        break;
                    }

                case SystemMessage.MessageType.SyncTracked:
                    {
                        string[] point = aMessage.Data.Split(' ');
                        if (point.Length >= 3)
                        {
                            float x = float.Parse(point[0]);
                            float y = float.Parse(point[1]);
                            float z = float.Parse(point[2]);
                            KinectManager.Default.FavorNearest(x, y, z);
                        }
                        else
                        {
                            log.Error("Invalid favor point");
                        }

                        break;
                    }

                default:
                    {
                        Debug.WriteLine("SystemWatchdog - LOG - Did receive message of type: " + aMessage.Type.ToString() + " data: " + aMessage.Data);
                        break;
                    }
            }
        }
    }
}
