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

namespace KinectShowcaseCommon.ProcessHandling
{

    // Canaries were brought into mineshafts to detect posionus gases
    // This one detects when the program is done :)
    public class SystemCanary : ISystemInteractionListener
    {
        private static volatile SystemCanary _instance;
        private static object _syncRoot = new Object();

        public ISystemProgressListener ProgessListener;

        public bool Connected { get; private set; }
        private AnonymousPipeClientStream _pipeStream = null;
        private StreamWriter _streamWriter;

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
            this.Connected = false;
        }

        ~SystemCanary()
        {
            if (this.Connected)
            {
                this._streamWriter.Close();
                this._streamWriter = null;
                this.Connected = false;
            }
        }

        public void DidStartWithStreamHandle(string aHandle)
        {
            if (!this.Connected)
            {
                try
                {
                    _pipeStream = new AnonymousPipeClientStream(PipeDirection.Out, aHandle);
                    _streamWriter = new StreamWriter(_pipeStream);
                    _streamWriter.AutoFlush = true;

                    SystemMessage pingMes = new SystemMessage(SystemMessage.MessageType.Ping, DateTime.Now.ToString());
                    this._streamWriter.WriteLine(pingMes.String());

                    this.Connected = true;
                }
                catch (TimeoutException e)
                {
                    Debug.WriteLine("SystemCanary - WARN - Couldn't connect to watch dog");
                }
            }
        }

        public void SystemDidRecieveInteraction()
        {
            if (this.Connected)
            {
                SystemMessage interactMes = new SystemMessage(SystemMessage.MessageType.Interaction, DateTime.Now.ToString());
                this._streamWriter.WriteLine(interactMes.String());
            }
        }

        public void AskForKill()
        {
            if (this.Connected)
            {
                SystemMessage killMe = new SystemMessage(SystemMessage.MessageType.Kill, DateTime.Now.ToString());
                this._streamWriter.WriteLine(killMe.String());
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}
