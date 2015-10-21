using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.ProcessHandling
{
    public class SystemMessage
    {
        private const string SEPARATOR = ", ";
        private const string MES_TYPE_UNKNOWN = "MES_TYPE_UNKNOWN";
        private const string MES_TYPE_PING = "MES_TYPE_PING";
        private const string MES_TYPE_INTERACT = "MES_TYPE_INTERACT";
        private const string MES_TYPE_KILL = "MES_TYPE_KILL";

        public enum MessageType
        {
            Unknown,
            Ping,
            Interaction,
            Kill,
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
                MessageType type = SystemMessage.MessageTypeFromString(messageTypeString);
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
            return SystemMessage.MessageTypeToString(this.Type) + SEPARATOR + this.Data;
        }

        public static string MessageTypeToString(MessageType aType)
        {
            string result = MES_TYPE_UNKNOWN;
            if (aType == MessageType.Ping)
            {
                result = MES_TYPE_PING;
            }
            else if (aType == MessageType.Interaction)
            {
                result = MES_TYPE_INTERACT;
            }
            else if (aType == MessageType.Kill)
            {
                result = MES_TYPE_KILL;
            }

            return result;
        }

        public static MessageType MessageTypeFromString(string aType)
        {
            MessageType result = MessageType.Unknown;
            if (aType.Equals(MES_TYPE_PING))
            {
                result = MessageType.Ping;
            }
            else if (aType.Equals(MES_TYPE_INTERACT))
            {
                result = MessageType.Interaction;
            }
            else if (aType.Equals(MES_TYPE_KILL))
            {
                result = MessageType.Kill;
            }

            return result;
        }
    }
}
