using log4net;
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
    public class IPCServer : IPCHandler
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //server child process connect to to send progress updates
        private AnonymousPipeServerStream _inServer, _outServer;

        public IPCServer()
        {

        }

        override public void Start()
        {
            //create the server that client processes will connect to
            _inServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            //create a stream reader that will read the bytes from the stream
            this.StreamReader = new StreamReader(_inServer);

            //create another server for sending
            _outServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            this.StreamWriter = new StreamWriter(_outServer);
            this.StreamWriter.AutoFlush = true;

            base.Start();
        }

        public string GetInServerClientHandle()
        {
            return _inServer.GetClientHandleAsString();
        }

        public string GetOutServerClientHandle()
        {
            return _outServer.GetClientHandleAsString();
        }

        override public void Close()
        {
            base.Close();

            if (_inServer != null)
            {
                _inServer.DisposeLocalCopyOfClientHandle();
                _inServer.Close();
                _inServer = null;
            }

            if (_outServer != null)
            {
                _outServer.DisposeLocalCopyOfClientHandle();
                _outServer.Close();
                _outServer = null;
            }
        }
    }
}
