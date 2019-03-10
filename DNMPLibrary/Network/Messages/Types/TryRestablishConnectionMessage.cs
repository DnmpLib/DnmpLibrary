using System.IO;

namespace DNMPLibrary.Network.Messages.Types
{
    internal class TryRestablishConnectionMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.TryRestablishConnection;

        public ushort[] ClientIds;

        public TryRestablishConnectionMessage(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));
            ClientIds = new ushort[reader.ReadUInt16()];
            for (var i = 0; i < ClientIds.Length; i++)
                ClientIds[i] = reader.ReadUInt16();
        }

        public TryRestablishConnectionMessage(ushort[] clientIds)
        {
            ClientIds = clientIds;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);

            writer.Write((ushort)ClientIds.Length);
            foreach (var id in ClientIds)
                writer.Write(id);

            return memoryStream.ToArray();
        }
    }
}
//TryRestablishConnectionMessage