using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
{
    internal class DataBroadcastMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.DataBroadcast;

        public byte[] Payload;

        public DataBroadcastMessage(byte[] data)
        {
            Payload = data;
        }

        public DataBroadcastMessage(byte[] data, bool packetCreation = false) //-V3117
        {
            Payload = data;
        }

        public byte[] GetBytes()
        {
            return Payload;
        }
    }
}
