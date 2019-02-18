using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
{
    internal class PingMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.Ping;

        public byte[] Payload;

        public PingMessage(byte[] data)
        {
            Payload = data;
        }

        public byte[] GetBytes()
        {
            return Payload;
        }
    }
}
