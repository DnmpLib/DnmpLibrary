using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
{
    internal class HeartbeatMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.Heartbeat;

        public byte[] GetBytes() => new byte[0];
    }
}
