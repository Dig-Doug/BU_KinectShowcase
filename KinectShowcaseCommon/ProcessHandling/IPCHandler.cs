using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.ProcessHandling
{
    public abstract class IPCHandler
    {
        public interface MessageReceiver
        {
            void ReceivedMessage(SystemMessage aMessage);
        }

        public MessageReceiver Receiver { get; set; }

        //stream reader for messages from the processes
        protected StreamReader StreamReader;
        protected StreamWriter StreamWriter;
        //threads for watching and talking with processes
        private Thread _readThread;
        private volatile bool _shouldStop = false;

        public virtual void Start()
        {
            //create a thread to receiver messages from the client
            _readThread = new Thread(new ThreadStart(ReceiveMessages));
            _readThread.Start();
        }

        public void SendMessage(SystemMessage aMessage)
        {
            if (this.StreamWriter != null)
            {
                this.StreamWriter.WriteLine(aMessage.String());
            }
            else
            {
                Debug.WriteLine("Not connected!");
            }
        }

        public bool IsConnected()
        {
            return StreamReader != null || StreamWriter != null;
        }

        public bool CanSend()
        {
            return StreamWriter != null;
        }

        public bool CanRead()
        {
            return StreamReader != null;
        }

        private void ReceiveMessages()
        {
            while (!_shouldStop)
            {
                string temp;
                //see if we can read any lines from the stream
                if ((temp = StreamReader.ReadLine()) != null)
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

        public virtual void Close()
        {
            _shouldStop = true;

            if (StreamReader != null)
            {
                StreamReader.Close();
                StreamReader = null;
            }

            if (StreamWriter != null)
            {
                StreamWriter.Close();
                StreamWriter = null;
            }

            if (_readThread != null)
            {
                //stop thread
                _readThread.Abort();
                //wait for thread to finish
                _readThread.Join();
            }
        }
    }
}
