using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnmpLibrary.Util.BigEndian;

namespace DnmpLibrary.Network.Messages.Types
{
    internal class SubnetworkSpanningUpdateMessage : ITypedMessage
    {
        public KeyValuePair<ushort, ushort>[] Clients;

        public SubnetworkSpanningUpdateMessage(byte[] data)
        {
            var memoryStream = new MemoryStream(data);
            var reader = new BigEndianBinaryReader(memoryStream);
            var length = reader.ReadUInt16();
            Clients = new KeyValuePair<ushort, ushort>[length];
            Clients = Clients.Select(x => new KeyValuePair<ushort, ushort>(reader.ReadUInt16(), reader.ReadUInt16())).ToArray();
        }

        public SubnetworkSpanningUpdateMessage(KeyValuePair<ushort, ushort>[] clients)
        {
            Clients = clients;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);
            writer.Write((ushort)Clients.Length);
            foreach(var client in Clients)
            {
                writer.Write(client.Key);
                writer.Write(client.Value);
            }
            return memoryStream.ToArray();
        }

        public MessageType GetMessageType() => MessageType.SubnetworkSpanningUpdate;
    }
}
