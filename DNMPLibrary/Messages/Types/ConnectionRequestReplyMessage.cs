using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
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
