using System.Collections.Generic;
using System.IO;
using System.Linq;
using DNMPLibrary.Util.BigEndian;

namespace DNMPLibrary.Network.Messages.Types
{
    internal class BroadcastedSubnetworkSpanningUpdateMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.BroadcastedSubnetworkSpanningUpdate;

        public KeyValuePair<ushort, ushort>[] Clients;

        public BroadcastedSubnetworkSpanningUpdateMessage(byte[] data)
        {
            var memoryStream = new MemoryStream(data);
            var reader = new BigEndianBinaryReader(memoryStream);
            var length = reader.ReadUInt16();
            Clients = new KeyValuePair<ushort, ushort>[length];
            Clients = Clients.Select(x => new KeyValuePair<ushort, ushort>(reader.ReadUInt16(), reader.ReadUInt16())).ToArray();
        }

        public BroadcastedSubnetworkSpanningUpdateMessage(KeyValuePair<ushort, ushort>[] clients)
        {
            Clients = clients;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);
            writer.Write((ushort)Clients.Length);
            foreach (var client in Clients)
            {
                writer.Write(client.Key);
                writer.Write(client.Value);
            }
            return memoryStream.ToArray();
        }
    }
}
