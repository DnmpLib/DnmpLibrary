using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
{
    internal class PongMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.Pong;

        public byte[] Payload;

        public PongMessage(byte[] data)
        {
            Payload = data;
        }

        public byte[] GetBytes()
        {
            return Payload;
        }
    }
}
