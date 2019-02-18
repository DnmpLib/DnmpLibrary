using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DNMPLibrary.Security;

namespace DNMPLibrary.Messages.Types
{
    internal class ConnectionRequestConfirmMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionRequestConfirm;

        public byte[] EncryptedToken;

        public ConnectionRequestConfirmMessage(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));
            EncryptedToken = reader.ReadBytes(reader.ReadUInt16());
        }

        public ConnectionRequestConfirmMessage() { }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);

            writer.Write((ushort) EncryptedToken.Length);
            writer.Write(EncryptedToken);

            return memoryStream.ToArray();
        }
    }
}
