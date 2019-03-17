using System.Collections.Generic;
using System.IO;
using System.Linq;
using DNMPLibrary.Core;
using DNMPLibrary.Interaction.Protocol;
using DNMPLibrary.Util.BigEndian;

namespace DNMPLibrary.Network.Messages.Types
{
    internal class ConnectionRequestConfirmReplyMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionRequestConfirmReply;

        public readonly List<DNMPNode> Clients = new List<DNMPNode>();
        public readonly ushort NewId;
        public readonly IEndPoint NewEndPoint;

        private readonly IEndPointFactory endPointFactory;

        public ConnectionRequestConfirmReplyMessage(byte[] data, IEndPointFactory endPointFactory)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(data));

            NewId = reader.ReadUInt16();
            NewEndPoint = endPointFactory.DeserializeEndPoint(reader.ReadBytes(reader.ReadUInt16()));

            var clientCount = reader.ReadUInt16();
            for (var i = 0; i < clientCount; i++)
                Clients.Add(new DNMPNode
                {
                    Id = reader.ReadUInt16(),
                    ParentId = reader.ReadUInt16(),
                    EndPoint = endPointFactory.DeserializeEndPoint(reader.ReadBytes(reader.ReadUInt16()))
                });
        }

        public ConnectionRequestConfirmReplyMessage(List<DNMPNode> clients, ushort newId, IEndPoint newEndPoint, IEndPointFactory endPointFactory)
        {
            Clients = clients;
            NewId = newId;
            NewEndPoint = newEndPoint;
            this.endPointFactory = endPointFactory;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);

            writer.Write(NewId);
            
            var selfBuf = endPointFactory.SerializeEndPoint(NewEndPoint);
            if (selfBuf.Length > ushort.MaxValue)
                throw new DNMPException("buf.Length larger then ushort");
            writer.Write((ushort)selfBuf.Length);
            writer.Write(selfBuf);

            writer.Write((ushort)Clients.Count(x => x.Id != NewId));
            foreach (var client in Clients)
            {
                if (client.Id == NewId)
                    continue;
                writer.Write(client.Id);
                writer.Write(client.ParentId);
                var buf = endPointFactory.SerializeEndPoint(client.EndPoint);
                if (buf.Length > ushort.MaxValue)
                    throw new DNMPException("buf.Length larger then ushort");
                writer.Write((ushort)buf.Length);
                writer.Write(buf);
                writer.Write((ushort)client.CustomData.Length);
                writer.Write(client.CustomData);
            }

            return memoryStream.ToArray();
        }
    }
}
