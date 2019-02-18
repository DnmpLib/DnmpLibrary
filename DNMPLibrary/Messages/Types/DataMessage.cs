using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
{
    internal class DataMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.Data;

        public byte[] Payload;

        public DataMessage(byte[] data)
        {
            Payload = data;
        }

        public DataMessage(byte[] data, bool packetCreation = false) //-V3117
        {
            Payload = data;
        }

        public byte[] GetBytes()
        {
            return Payload;
        }
    }
}
