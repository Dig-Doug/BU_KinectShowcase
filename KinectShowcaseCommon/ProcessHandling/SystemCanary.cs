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

        private IPCSender _sender;

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
            _sender = new IPCSender();
        }

        ~SystemCanary()
        {
            _sender.Close();
        }

        public void DidStartWithStreamHandle(string aHandle)
        {
            _sender.ConnectWithStreamHandle(aHandle);
        }

        public void SystemDidRecieveInteraction()
        {
            if (_sender.Connected)
            {
                SystemMessage interactMes = new SystemMessage(SystemMessage.MessageType.Interaction, DateTime.Now.ToString());
                _sender.SendMessage(interactMes);
            }
        }

        public void AskForKill()
        {
            if (_sender.Connected)
            {
                SystemMessage killMe = new SystemMessage(SystemMessage.MessageType.Kill, DateTime.Now.ToString());
                _sender.SendMessage(killMe);
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}
