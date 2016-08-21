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
using Microsoft.Kinect;
using Grpc.Core;

namespace KinectShowcaseCommon.ProcessHandling
{
    // Canaries were brought into mineshafts to detect posionous gases
    // This one detects when the program is done :)
    public class SystemCanary : Ipc.Slave.SlaveBase, ISystemInteractionListener
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const float TIME_BETWEEN_UPDATES = 1.0f * 1000.0f; //milliseconds

        private static volatile SystemCanary _instance;
        private static object _syncRoot = new Object();

        public ISystemProgressListener ProgessListener;

        private DateTime _lastInteractionUpdateTime = DateTime.Now;

        private Server _grpcServer;
        private Channel _grpcChannel;
        private Ipc.Master.MasterClient _masterClient;

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
        }

        ~SystemCanary()
        {
            _grpcChannel.ShutdownAsync().Wait();
        }

        public void StartGRPC(int masterPort, int slavePort)
        {
            // Get a client for the master
            _grpcChannel = new Channel("127.0.0.1:" + masterPort, ChannelCredentials.Insecure);
            _masterClient = new Ipc.Master.MasterClient(_grpcChannel);
            // Start the gRPC server for control
            _grpcServer = new Server
            {
                Services = { Ipc.Slave.BindService(this) },
                Ports = { new ServerPort("localhost", slavePort, ServerCredentials.Insecure) }
            };
            _grpcServer.Start();
        }

        public void SystemDidRecieveInteraction()
        {
            if ((DateTime.Now - _lastInteractionUpdateTime).TotalMilliseconds > TIME_BETWEEN_UPDATES)
            {
                _lastInteractionUpdateTime = DateTime.Now;

                try
                {
                    _masterClient.KeepAliveAsync(new Ipc.KeepAliveRequest
                    {
                        Time = 10 // TODO(doug) - This should be changed if the time field is used
                    }, deadline: DateTime.UtcNow.AddMilliseconds(TIME_BETWEEN_UPDATES));
                }
                catch (Grpc.Core.RpcException e)
                {
                    // Expected when there's no master
                    log.Warn(e.Message);
                }
            }
        }

        public void AskForKill()
        {
            /*
            log.Info("Asking for kill");
            //send state
            string handLoc = KinectManager.Default.HandManager.HandPosition.X + " " + KinectManager.Default.HandManager.HandPosition.Y;
            SystemMessage syncHand = new SystemMessage(SystemMessage.MessageType.SyncHand, handLoc);
            _client.SendMessage(syncHand);

            CameraSpacePoint trackedLoc = KinectManager.Default.GetTrackedLocation();
            string tracked = "" + trackedLoc.X + " " + trackedLoc.Y + " " + trackedLoc.Z;
            SystemMessage syncTracked = new SystemMessage(SystemMessage.MessageType.SyncTracked, tracked);
            _client.SendMessage(syncTracked);

            if (_client.CanSend())
            {
                SystemMessage killMe = new SystemMessage(SystemMessage.MessageType.Kill, DateTime.Now.ToString());
                _client.SendMessage(killMe);
            }
            else
            {
                Application.Current.Shutdown();
            }
            */
            Application.Current.Shutdown();
        }

        /*
        public void ReceivedMessage(SystemMessage aMessage)
        {
            switch (aMessage.Type)
            {
                case SystemMessage.MessageType.Ping:
                    {
                        log.Info("Received ping from client");
                        break;
                    }

                case SystemMessage.MessageType.SyncHand:
                    {
                        string[] point = aMessage.Data.Split(' ');
                        if (point.Length >= 2)
                        {
                            float x = float.Parse(point[0]);
                            float y = float.Parse(point[1]);
                            log.Info("Received hand sync X: " + x + " Y: " + y);
                            KinectManager.Default.HandManager.SetScaledHandLocation(new Point(x, y));
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
                            log.Info("Received tracked sync X: " + x + " Y: " + y + " Z: " + z);
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
                        log.Info("Did receive message of type: " + aMessage.Type.ToString() + " data: " + aMessage.Data);
                        break;
                    }
            }
        }
        */
    }
}
