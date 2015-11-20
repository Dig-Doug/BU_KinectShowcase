using KinectShowcaseCommon.Kinect_Processing;
using Microsoft.Kinect;
using System;
using System.Diagnostics;
using System.Threading;

namespace KinectShowcaseCommon.ProcessHandling
{
    public sealed class SystemWatchdog : ISystemInteractionListener, IPCReceiver.MessageReceiver
    {
        //max timeout for child processes, if interaction time get larger than this, the child is killed
        private const float INTERACTION_TIME_THRESHOLD = 0.25f * 60.0f;

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

        private IPCSender _sender;
        private IPCReceiver _receiver;

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

        private SystemWatchdog()
        {
            _receiver = new IPCReceiver();
            _receiver.Receiver = this;
            _receiver.Start();

            Debug.WriteLine("Pipe server handle: " + _receiver.GetPipeServerClientHandle());

            _sender = new IPCSender();

            //start the general managing thread
            _generalThread = new Thread(new ThreadStart(ProgramManagement_Main));
            _generalThread.Start();
        }

        public void OnExit()
        {
            _receiver.Close();

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
            try
            {
                bool shouldQuit = false;
                while (!shouldQuit)
                {
                    //only manage if we don't have a child process
                    if (this._childProcess == null)
                    {
                        //check if we've passed the timeout limit
                        if ((DateTime.Now - _lastInteractionTime).TotalSeconds >= INTERACTION_TIME_THRESHOLD)
                        {
                            //reset to the home screen
                            Debug.WriteLine("SystemWatchdog - LOG - System timed out, returning to the home screen");
                            ProgramManagement_GoHome();
                            _lastInteractionTime = DateTime.Now;
                        }
                    }

                    //wait for a bit
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
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
                Debug.WriteLine("No navigationhandler set!");
            }
        }

        #endregion

        #region Process Management

        public void StartChildProcess(string aExecutablePath)
        {
            Debug.WriteLine("SystemWatchdog - LOG - Staring process exe: " + aExecutablePath);

            //clean up any other child processes if there are any
            if (this._childProcess != null)
            {
                if (!_childProcess.HasExited)
                {
                    Debug.WriteLine("SystemWatchdog - LOG - Already a child process running... killing");
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

            CameraSpacePoint trackedLoc = KinectManager.Default.GetTrackedLocation();
            string args = _receiver.GetPipeServerClientHandle() + " " + KinectManager.Default.HandManager.HandPosition.X + " " + KinectManager.Default.HandManager.HandPosition.Y;
            args += " " + trackedLoc.X + " " + trackedLoc.Y + " " + trackedLoc.Z;

            _childProcess.StartInfo.Arguments = args;
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
            //wait until the server has connected to a client
            while (!_receiver.IsConnected()) ;

            bool shouldQuit = false;
            while (!shouldQuit)
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

        public void ReceivedMessage(SystemMessage aMessage)
        {
            if (aMessage.Type == SystemMessage.MessageType.Interaction)
            {
                Debug.WriteLine("SystemWatchdog - LOG - received interaction from client");
                _lastInteractionTime = DateTime.Now;
            }
            else if (aMessage.Type == SystemMessage.MessageType.Ping)
            {
                Debug.WriteLine("SystemWatchdog - LOG - received ping from client");
            }
            else if (aMessage.Type == SystemMessage.MessageType.Kill)
            {
                Debug.WriteLine("SystemWatchdog - LOG - received kill command from client");

                lock (_childProcessLock)
                {
                    _childProcess.Kill();
                    _childProcess = null;
                }
            }
            else
            {
                Debug.WriteLine("SystemWatchdog - LOG - Did receive message of type: " + SystemMessage.MessageTypeToString(aMessage.Type) + " data: " + aMessage.Data);
            }
        }

        #endregion
    }
}
