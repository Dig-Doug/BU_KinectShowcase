using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.ProcessHandling
{
    public class IPCSender
    {
        public bool Connected { get; private set; }
        private AnonymousPipeClientStream _pipeStream = null;
        private StreamWriter _streamWriter;

        public IPCSender()
        {
            this.Connected = false;
        }

        public void ConnectWithStreamHandle(string aHandle)
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
                    Debug.WriteLine("Couldn't connect to handle");
                }
            }
        }

        public void Close()
        {
            if (this.Connected)
            {
                this._streamWriter.Close();
                this._streamWriter = null;
                this.Connected = false;
            }
        }

        public void SendMessage(SystemMessage aMessage)
        {
            if (this.Connected)
            {
                this._streamWriter.WriteLine(aMessage.String());
            }
            else
            {
                Debug.WriteLine("Not connected!");
            }
        }
    }
}
