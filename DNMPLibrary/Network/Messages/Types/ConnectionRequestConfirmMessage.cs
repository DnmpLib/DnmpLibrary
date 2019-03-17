using System.IO;
using DNMPLibrary.Util.BigEndian;

namespace DNMPLibrary.Network.Messages.Types
{
    internal class ConnectionRequestConfirmMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionRequestConfirm;

        public byte[] EncryptedToken;

        public ConnectionRequestConfirmMessage(byte[] data)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(data));
            EncryptedToken = reader.ReadBytes(reader.ReadUInt16());
        }

        public ConnectionRequestConfirmMessage() { }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);

            writer.Write((ushort) EncryptedToken.Length);
            writer.Write(EncryptedToken);

            return memoryStream.ToArray();
        }
    }
}
