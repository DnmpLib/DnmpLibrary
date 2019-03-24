using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnmpLibrary.Core;
using DnmpLibrary.Interaction.Protocol;
using DnmpLibrary.Security.Cryptography.Symmetric;
using DnmpLibrary.Util.BigEndian;

namespace DnmpLibrary.Network.Messages.Types
{
    internal class ConnectionRequestConfirmReplyMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionRequestConfirmReply;

        public readonly List<DnmpNode> Clients = new List<DnmpNode>();
        public readonly ushort NewId;
        public readonly IEndPoint NewEndPoint;

        private readonly IEndPointFactory endPointFactory;
        private readonly ISymmetricKey key;

        public ConnectionRequestConfirmReplyMessage(byte[] data, IEndPointFactory endPointFactory, ISymmetricKey key)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(data));

            var tLen = reader.ReadInt32();
            if (tLen > 20 * 1024 * 1024)
                throw new DnmpException("tLen is larger than 20MiB");
            var dataReader = new BigEndianBinaryReader(new MemoryStream(SymmetricHelper.Decrypt(key, reader.ReadBytes(tLen))));

            NewId = dataReader.ReadUInt16();
            NewEndPoint = endPointFactory.DeserializeEndPoint(dataReader.ReadBytes(dataReader.ReadUInt16()));

            var clientCount = dataReader.ReadUInt16();
            for (var i = 0; i < clientCount; i++)
                Clients.Add(new DnmpNode
                {
                    Id = dataReader.ReadUInt16(),
                    ParentId = dataReader.ReadUInt16(),
                    EndPoint = endPointFactory.DeserializeEndPoint(dataReader.ReadBytes(dataReader.ReadUInt16())),
                    CustomData = dataReader.ReadBytes(reader.ReadUInt16())
                });
        }

        public ConnectionRequestConfirmReplyMessage(List<DnmpNode> clients, ushort newId, IEndPoint newEndPoint, IEndPointFactory endPointFactory, ISymmetricKey key)
        {
            Clients = clients;
            NewId = newId;
            NewEndPoint = newEndPoint;
            this.endPointFactory = endPointFactory;
            this.key = key;
        }

        public byte[] GetBytes()
        {
            var dataMemoryStream = new MemoryStream();
            var dataWriter = new BigEndianBinaryWriter(dataMemoryStream);

            dataWriter.Write(NewId);
            
            var selfBuf = endPointFactory.SerializeEndPoint(NewEndPoint);
            if (selfBuf.Length > ushort.MaxValue)
                throw new DnmpException("buf.Length larger then ushort");
            dataWriter.Write((ushort)selfBuf.Length);
            dataWriter.Write(selfBuf);

            dataWriter.Write((ushort)Clients.Count(x => x.Id != NewId));
            foreach (var client in Clients)
            {
                if (client.Id == NewId)
                    continue;
                dataWriter.Write(client.Id);
                dataWriter.Write(client.ParentId);
                var buf = endPointFactory.SerializeEndPoint(client.EndPoint);
                if (buf.Length > ushort.MaxValue)
                    throw new DnmpException("buf.Length larger then ushort");
                dataWriter.Write((ushort)buf.Length);
                dataWriter.Write(buf);
                dataWriter.Write((ushort)client.CustomData.Length);
                dataWriter.Write(client.CustomData);
            }

            return SymmetricHelper.Encrypt(key, dataMemoryStream.ToArray());
        }
    }
}
