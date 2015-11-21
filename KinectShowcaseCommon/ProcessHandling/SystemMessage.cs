using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.ProcessHandling
{
    public class SystemMessage
    {
        private const string SEPARATOR = "~~~";

        public enum MessageType
        {
            Unknown,
            Ping,
            Ack,
            Interaction,
            Kill,
            ServerHandle,
            SyncHand,
            SyncTracked
        }

        public MessageType Type;
        public string Data;

        public static SystemMessage MessageFromString(string aString)
        {
            SystemMessage result = null;
            int separatorIndex = aString.IndexOf(SEPARATOR);
            if (separatorIndex != -1)
            {
                string messageTypeString = aString.Substring(0, separatorIndex);
                MessageType type = (MessageType)Enum.Parse(typeof(MessageType), messageTypeString);
                string data = aString.Substring(separatorIndex + SEPARATOR.Length - 1, aString.Length - (separatorIndex + SEPARATOR.Length - 1));
                result = new SystemMessage(type, data);
            }

            return result;
        }

        public SystemMessage(MessageType aMessageType, string aData)
        {
            this.Type = aMessageType;
            this.Data = aData;
        }

        public string String()
        {
            return this.Type.ToString() + SEPARATOR + this.Data;
        }
    }
}
