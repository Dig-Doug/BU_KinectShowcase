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
    public class IPCReceiver
    {
        public interface MessageReceiver
        {
            void ReceivedMessage(SystemMessage aMessage);
        }

        public MessageReceiver Receiver {get; set;}

        //server child process connect to to send progress updates
        private AnonymousPipeServerStream _pipeServer;
        //stream reader for messages from the processes
        private StreamReader _streamReader;
        //threads for watching and talking with processes
        private Thread _readThread;
        private bool _shouldQuit = false;

        public IPCReceiver()
        {

        }

        public void Start()
        {
            //create the server that client processes will connect to
            _pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            //create a stream reader that will read the bytes from the stream
            _streamReader = new StreamReader(_pipeServer);

            //create a thread to receiver messages from the client
            _readThread = new Thread(new ThreadStart(ReceiveMessages));
            _readThread.Start();
        }

        public string GetPipeServerClientHandle()
        {
            return _pipeServer.GetClientHandleAsString();
        }

        public bool IsConnected()
        {
            return _pipeServer.IsConnected;
        }

        public void Close()
        {
            if (_readThread != null)
            {
                //stop thread
                _readThread.Abort();
                //wait for thread to finish
                _readThread.Join();
            }

            if (_pipeServer != null)
            {
                _pipeServer.DisposeLocalCopyOfClientHandle();
                _pipeServer.Close();
                _pipeServer = null;
            }
        }

        private void ReceiveMessages()
        {
            bool shouldQuit = false;
            while (!shouldQuit)
            {
                //wait until the server has connected to a client
                while (!_pipeServer.IsConnected)
                {
                    Thread.Sleep(100);
                }

                string temp;
                //see if we can read any lines from the stream
                if ((temp = _streamReader.ReadLine()) != null)
                {
                    //process the message
                    SystemMessage message = SystemMessage.MessageFromString(temp);
                    if (this.Receiver != null)
                    {
                        this.Receiver.ReceivedMessage(message);
                    }
                    else
                    {
                        Debug.WriteLine("Unhandled message!");
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
