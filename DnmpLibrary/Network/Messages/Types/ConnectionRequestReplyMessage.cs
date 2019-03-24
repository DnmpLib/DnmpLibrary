using System.IO;
using DnmpLibrary.Util.BigEndian;

namespace DnmpLibrary.Network.Messages.Types
{
    internal class ConnectionRequestReplyMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionRequestReply;

        public byte[] Token;

        public ConnectionRequestReplyMessage() { }

        public ConnectionRequestReplyMessage(byte[] data)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(data));
            Token = reader.ReadBytes(16);
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);
            writer.Write(Token);
            return memoryStream.ToArray();
        }
    }
}
