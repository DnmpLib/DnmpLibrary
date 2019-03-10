using System.IO;

namespace DNMPLibrary.Network.Messages.Types
{
    internal class ConnectionRequestReplyMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionRequestReply;

        public byte[] Token;

        public ConnectionRequestReplyMessage() { }

        public ConnectionRequestReplyMessage(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));
            Token = reader.ReadBytes(16);
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);
            writer.Write(Token);
            return memoryStream.ToArray();
        }
    }
}
