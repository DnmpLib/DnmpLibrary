using System.IO;
using DNMPLibrary.Util.BigEndian;

namespace DNMPLibrary.Network.Messages.Types
{
    internal class PingUpdateMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.PingUpdate;

        public ushort[] UnDirectClientIds;

        public PingUpdateMessage(byte[] data)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(data));
            UnDirectClientIds = new ushort[reader.ReadUInt16()];
            for (var i = 0; i < UnDirectClientIds.Length; i++)
                UnDirectClientIds[i] = reader.ReadUInt16();
        }

        public PingUpdateMessage(ushort[] unDirectClientIds)
        {
            UnDirectClientIds = unDirectClientIds;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);

            writer.Write((ushort) UnDirectClientIds.Length);
            foreach (var id in UnDirectClientIds)
                writer.Write(id);

            return memoryStream.ToArray();
        }
    }
}
