using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
{
    internal class DummyMessage : ITypedMessage
    {
        private readonly BaseMessage message;

        public DummyMessage(BaseMessage message)
        {
            this.message = message;
        }

        public MessageType GetMessageType() => message.MessageType;

        public byte[] GetBytes() => message.Payload;
    }
}
