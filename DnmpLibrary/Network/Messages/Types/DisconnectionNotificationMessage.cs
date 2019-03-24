using System.IO;
using DnmpLibrary.Util.BigEndian;

namespace DnmpLibrary.Network.Messages.Types
{
    internal class DisconnectionNotificationMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.DisconnectionNotification;

        public readonly ushort Id;

        public DisconnectionNotificationMessage(byte[] data)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(data));

            Id = reader.ReadUInt16();
        }

        public DisconnectionNotificationMessage(ushort id)
        {
            Id = id;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);

            writer.Write(Id);

            return memoryStream.ToArray();
        }
    }
}
