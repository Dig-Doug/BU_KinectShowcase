using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace KinectShowcaseCommon.ProcessHandling
{
    public class IPCClient : IPCHandler
    {
        private AnonymousPipeClientStream _outStream = null, _inStream;

        public IPCClient()
        {

        }

        override public void Start()
        {
            throw new NotImplementedException("Don't call start");
        }

        public void ConnectWithStreamHandles(string aInHandle, string aOutHandle)
        {
            try
            {
                _outStream = new AnonymousPipeClientStream(PipeDirection.Out, aOutHandle);
                this.StreamWriter = new StreamWriter(_outStream);
                this.StreamWriter.AutoFlush = true;

                _inStream = new AnonymousPipeClientStream(PipeDirection.In, aInHandle);
                //create a stream reader that will read the bytes from the stream
                this.StreamReader = new StreamReader(_inStream);

                base.Start();
            }
            catch (TimeoutException e)
            {
                Debug.WriteLine("Couldn't connect to handle");
            }
        }

        override public void Close()
        {
            base.Close();

            if (_outStream != null)
            {
                _outStream.Close();
                _outStream = null;
            }

            if (_inStream != null)
            {
                _inStream.Close();
                _inStream = null;
            }
        }
    }
}
