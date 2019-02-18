using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
{
    internal class SubnetworkSpanningUpdateMessage : ITypedMessage
    {
        public KeyValuePair<ushort, ushort>[] Clients;

        public SubnetworkSpanningUpdateMessage(byte[] data)
        {
            var memoryStream = new MemoryStream(data);
            var reader = new BinaryReader(memoryStream);
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
            var writer = new BinaryWriter(memoryStream);
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
