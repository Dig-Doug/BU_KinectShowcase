using KinectShowcaseCommon.Kinect_Processing;
using log4net;
using Microsoft.Kinect;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using Grpc.Core;
using Ipc;
using System.Threading.Tasks;

/**
 * Regenerate protos with: ..\..\..\..\packages\Grpc.Tools.0.15.0\tools\windows_x86\protoc.exe -I../../../proto --csharp_out=../../../src-gen --grpc_out ../../../src-gen  ../../../proto/ipc.proto --plugin=protoc-gen-grpc=..\..\..\..\packages\Grpc.Tools.0.15.0\tools\windows_x86\grpc_csharp_plugin.exe
 */

namespace KinectShowcaseCommon.ProcessHandling
{
    public sealed class SystemWatchdog : Ipc.Master.MasterBase, ISystemInteractionListener
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //max timeout for child processes, if interaction time get larger than this, the child is killed
        private const float INTERACTION_TIME_THRESHOLD = 0.25f * 60.0f;
        public const int MASTER_PORT = 22000;
        public const int SLAVE_PORT = 22001;

        //singleton stuffs
        private static volatile SystemWatchdog _instance;
        private static object _syncRoot = new Object();

        public ISystemProgressListener ProgessListener;
        public ISystemTimeoutListener NavigationHandler;

        //holds the last time a user interacted with the system
        private DateTime _lastInteractionTime = DateTime.Now;

        //reference to the currently running child process
        private Process _childProcess;
        //lock for the child process object
        private Object _childProcessLock = new Object();
        //threads for watching and talking with processes
        private Thread _watchThread, _generalThread;

        private volatile bool _shouldStop = false;

        private Server _grpcServer;

        //singleton watchdog
        public static SystemWatchdog Default
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                            _instance = new SystemWatchdog();
                    }
                }

                return _instance;
            }
        }

        public override Task<KeepAliveResponse> KeepAlive(KeepAliveRequest request, ServerCallContext context)
        {
            // Store interaction time
            this._lastInteractionTime = DateTime.Now;

            KeepAliveResponse response = new KeepAliveResponse
            {
                
            };
            return Task.FromResult(response);
        }

        private SystemWatchdog()
        {
            //start the general managing thread
            _generalThread = new Thread(new ThreadStart(ProgramManagement_Main));
            _generalThread.Start();

            // Start the gRPC server so slaves can connect
            _grpcServer = new Server
            {
                Services = { Ipc.Master.BindService(this) },
                Ports = { new ServerPort("localhost", MASTER_PORT, ServerCredentials.Insecure) }
            };
            _grpcServer.Start();
        }

        public void OnExit()
        {
            _shouldStop = true;

            // Shutdown the gRPC server
            _grpcServer.ShutdownAsync().Wait();

            if (this._generalThread != null)
            {
                this._generalThread.Abort();
                this._generalThread.Join();
            }

            if (this._watchThread != null)
            {
                this._watchThread.Abort();
                this._watchThread.Join();
            }
        }

        #region Program Management

        private void ProgramManagement_Main()
        {
            while (!_shouldStop)
            {
                //only manage if we don't have a child process
                if (this._childProcess == null)
                {
                    //check if we've passed the timeout limit
                    if ((DateTime.Now - _lastInteractionTime).TotalSeconds >= INTERACTION_TIME_THRESHOLD)
                    {
                        //reset to the home screen
                        log.Info("System timed out, returning to the home screen");
                        ProgramManagement_GoHome();
                        _lastInteractionTime = DateTime.Now;
                    }
                }

                //wait for a bit
                Thread.Sleep(100);
            }
        }

        private void ProgramManagement_GoHome()
        {
            if (this.NavigationHandler != null)
            {
                this.NavigationHandler.Reset();
            }
            else
            {
                log.Warn("No navigationhandler set!");
            }
        }

        #endregion

        #region Process Management

        public void StartChildProcess(string aExecutablePath)
        {
            log.Info("Staring process exe: " + aExecutablePath);

            //clean up any other child processes if there are any
            if (this._childProcess != null)
            {
                if (!_childProcess.HasExited)
                {
                    log.Warn("Already a child process running... killing");
                    this._childProcess.Kill();
                }
            }
            if (this._watchThread != null)
            {
                this._watchThread.Abort();
                this._watchThread.Join();
            }

            //create the new child process
            _childProcess = new Process();
            _childProcess.StartInfo.FileName = aExecutablePath;
            _childProcess.StartInfo.UseShellExecute = false;

            _childProcess.StartInfo.Arguments = "--master_port " + MASTER_PORT + " --slave_port " + SLAVE_PORT;
            _childProcess.Start();

            //create a thread to watch the child process
            _watchThread = new Thread(new ThreadStart(WatchProcess));
            _watchThread.Start();
            //set our interaction time
            _lastInteractionTime = DateTime.Now;

            //stop all kinect UI events from being passed on
            KinectManager.Default.ShouldSendEvents = false;
        }

        private void WatchProcess()
        {
            bool shouldQuit = false;
            while (!_shouldStop && !shouldQuit)
            {
                //lock the process
                lock (_childProcessLock)
                {
                    //see if we should quit
                    shouldQuit = _childProcess == null || _childProcess.HasExited || !_childProcess.Responding;
                }

                //check if we've passed the timeout limit
                if ((DateTime.Now - _lastInteractionTime).TotalSeconds >= INTERACTION_TIME_THRESHOLD)
                {
                    //lock and kill the process
                    lock (_childProcessLock)
                    {
                        log.Info("Timeout - Killing process");
                        _childProcess.Kill();
                        _childProcess = null;
                    }
                    break;
                }
            }

            //tell the kinect manager we're all done
            KinectManager.Default.ShouldSendEvents = true;
        }

        public void SystemDidRecieveInteraction()
        {
            this._lastInteractionTime = DateTime.Now;
        }

        /*
        public void ReceivedMessage(SystemMessage aMessage)
        {
            switch (aMessage.Type)
            {
                case SystemMessage.MessageType.Interaction:
                    {
                        log.Debug("Received interaction from client");
                        _lastInteractionTime = DateTime.Now;
                        break;
                    }

                case SystemMessage.MessageType.Ack:
                    {
                        log.Info("received ACK from client");
                        SystemMessage pingMes = new SystemMessage(SystemMessage.MessageType.Ping, DateTime.Now.ToString());
                        _server.SendMessage(pingMes);

                        Thread.Sleep(100);

                        //send state
                        string handLoc = KinectManager.Default.HandManager.HandPosition.X + " " + KinectManager.Default.HandManager.HandPosition.Y;
                        SystemMessage syncHand = new SystemMessage(SystemMessage.MessageType.SyncHand, handLoc);
                        _server.SendMessage(syncHand);

                        Thread.Sleep(100);

                        CameraSpacePoint trackedLoc = KinectManager.Default.GetTrackedLocation();
                        string tracked = "" + trackedLoc.X + " " + trackedLoc.Y + " " + trackedLoc.Z;
                        SystemMessage syncTracked = new SystemMessage(SystemMessage.MessageType.SyncTracked, tracked);
                        _server.SendMessage(syncTracked);

                        break;
                    }

                case SystemMessage.MessageType.Ping:
                    {
                        log.Info("received ping from client");
                        break;
                    }

                case SystemMessage.MessageType.Kill:
                    {
                        log.Info("received kill command from client");

                        lock (_childProcessLock)
                        {
                            if (_childProcess != null)
                            {
                                _childProcess.Kill();
                                _childProcess = null;
                            }
                        }
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

        #endregion
    }
}
