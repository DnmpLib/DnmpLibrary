using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
{
    internal class ReliableConfirmMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ReliableConfirm;

        public readonly Guid MessageId;

        public ReliableConfirmMessage(byte[] data)
        {
            MessageId = new Guid(data);
        }

        public ReliableConfirmMessage(Guid messageId)
        {
            MessageId = messageId;
        }

        public byte[] GetBytes() => MessageId.ToByteArray();
    }
}
