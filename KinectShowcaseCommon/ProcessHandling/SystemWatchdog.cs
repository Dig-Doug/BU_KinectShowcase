using KinectShowcaseCommon.Kinect_Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.ProcessHandling
{
    public sealed class SystemWatchdog : ISystemInteractionListener
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

        //server child process connect to to send progress updates
        private AnonymousPipeServerStream _pipeServer;
        //stream reader for messages from the processes
        private StreamReader _streamReader;
        //reference to the currently running child process
        private Process _childProcess;
        //lock for the child process object
        private Object _childProcessLock = new Object();
        //threads for watching and talking with processes
        private Thread _watchThread, _readThread, _generalThread;

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
            //create the server that client processes will connect to
            _pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            //create a stream reader that will read the bytes from the stream
            _streamReader = new StreamReader(_pipeServer);
            //start the general managing thread
            this.StartProgramManagement();
        }

        public void OnExit()
        {
            if (_pipeServer != null)
            {
                _pipeServer.DisposeLocalCopyOfClientHandle();
                _pipeServer.Close();
                _pipeServer = null;
            }

            if (this._generalThread != null && this._generalThread.IsAlive)
            {
                this._generalThread.Abort();
            }
        }

        #region Program Management

        private void StartProgramManagement()
        {
            _generalThread = new Thread(new ThreadStart(ProgramManagement_Main));
            _generalThread.Start();
        }

        private void ProgramManagement_Main()
        {
            while (true)
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
            if (this._watchThread != null && this._watchThread.IsAlive)
            {
                this._watchThread.Abort();
            }
            if (this._readThread != null && this._readThread.IsAlive)
            {
                this._readThread.Abort();
            }

            //create the new child process
            _childProcess = new Process();
            _childProcess.StartInfo.FileName = aExecutablePath;
            _childProcess.StartInfo.UseShellExecute = false;
            _childProcess.StartInfo.Arguments = _pipeServer.GetClientHandleAsString();
            _childProcess.Start();

            //create a thread to watch the child process
            _watchThread = new Thread(new ThreadStart(WatchProcess));
            _watchThread.Start();
            //create a thread to receiver messages from the client
            _readThread = new Thread(new ThreadStart(ReceiveMessages));
            _readThread.Start();
            //set our interaction time
            _lastInteractionTime = DateTime.Now;

            //stop all kinect UI events from being passed on
            KinectManager.Default.ShouldSendEvents = false;
        }

        private void WatchProcess()
        {
            //_pipeServer.WaitForPipeDrain();

            //wait until the server has connected to a client
            while (!_pipeServer.IsConnected) ;

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

        private void ReceiveMessages()
        {
            //wait until the server has connected to a client
            while (!_pipeServer.IsConnected) ;

            bool shouldQuit = false;
            while (!shouldQuit)
            {
                //lock the process
                lock (_childProcessLock)
                {
                    //see if we should quit
                    shouldQuit = _childProcess == null || _childProcess.HasExited || !_childProcess.Responding;
                }

                if (!shouldQuit)
                {
                    try
                    {
                        string temp;
                        //see if we can read any lines from the stream
                        if ((temp = _streamReader.ReadLine()) != null)
                        {
                            //process the message
                            SystemMessage message = SystemMessage.MessageFromString(temp);
                            this.DidGetMessage(message);
                        }
                    }
                    catch (ThreadAbortException e)
                    {
                        Debug.WriteLine("SystemWatchdog - WARN - Receiving thread was aborted");
                        shouldQuit = true;
                    }
                }
            }
        }

        public void SystemDidRecieveInteraction()
        {
            this._lastInteractionTime = DateTime.Now;
        }

        public void DidGetMessage(SystemMessage aMessage)
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
