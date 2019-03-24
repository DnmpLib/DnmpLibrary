using System;

namespace DnmpLibrary.Network.Messages.Types
{
    internal class TryRestablishConnectionReplyMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.TryRestablishConnectionReply;

        public bool Status;

        public TryRestablishConnectionReplyMessage(bool status)
        {
            Status = status;
        }

        public TryRestablishConnectionReplyMessage(byte[] data)
        {
            Status = BitConverter.ToBoolean(data, 0);
        }

        public byte[] GetBytes()
        {
            return BitConverter.GetBytes(Status);
        }
    }
}
