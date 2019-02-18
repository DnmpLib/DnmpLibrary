using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using DNMPLibrary.Client;
using DNMPLibrary.Core;
using DNMPLibrary.Security;
using DNMPLibrary.Util;

namespace DNMPLibrary.Messages.Types
{
    internal class ConnectionRequestConfirmReplyMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionRequestConfirmReply;

        public readonly List<DNMPClient> Clients = new List<DNMPClient>();
        public readonly ushort NewId;
        public readonly EndPoint NewEndPoint;
        public readonly ISymmetricKey SymmetricKey;
        private readonly IAsymmetricKey networkKey;


        public ConnectionRequestConfirmReplyMessage(byte[] data, ISymmetricKey dummySymmetricKey, IAsymmetricKey networkKey)
        {
            this.networkKey = networkKey;

            var rawReader = new BinaryReader(new MemoryStream(data));

            SymmetricKey = dummySymmetricKey.CreateFromBytes(AsymmetricHelper.Decrypt(this.networkKey, rawReader.ReadBytes(rawReader.ReadUInt16())));

            var encryptedDataSize = rawReader.ReadInt32();
            if (encryptedDataSize > 2621440)
                throw new DNMPException($"too large {nameof(encryptedDataSize)}");

            var decryptedData = SymmetricHelper.Decrypt(SymmetricKey, rawReader.ReadBytes(encryptedDataSize));

            var hash = decryptedData.Take(NetworkHashUtil.GetHashSize()).ToArray();
            decryptedData = decryptedData.Skip(NetworkHashUtil.GetHashSize()).ToArray();

            if (!NetworkHashUtil.ComputeChecksum(decryptedData).SequenceEqual(hash))
                throw new DNMPException("hash is not equal");

            var reader = new BinaryReader(new MemoryStream(decryptedData));

            NewId = reader.ReadUInt16();
            NewEndPoint = EndPointSerializer.FromBytes(reader.ReadBytes(reader.ReadUInt16()));

            var clientCount = reader.ReadUInt16();
            for (var i = 0; i < clientCount; i++)
                Clients.Add(new DNMPClient
                {
                    Id = reader.ReadUInt16(),
                    ParentId = reader.ReadUInt16(),
                    EndPoint = EndPointSerializer.FromBytes(reader.ReadBytes(reader.ReadUInt16()))
                });
        }

        public ConnectionRequestConfirmReplyMessage(ISymmetricKey symmetricKey, IAsymmetricKey networkKey, List<DNMPClient> clients, ushort newId, EndPoint newEndPoint)
        {
            Clients = clients;
            NewId = newId;
            SymmetricKey = symmetricKey;
            this.networkKey = networkKey;
            NewEndPoint = newEndPoint;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);

            var rawNetworkInfoMemoryStream = new MemoryStream();
            var rawWriter = new BinaryWriter(rawNetworkInfoMemoryStream);

            rawWriter.Write(NewId);
            
            var selfBuf = EndPointSerializer.ToBytes(NewEndPoint);
            if (selfBuf.Length > ushort.MaxValue) //-V3022
                throw new DNMPException("buf.Length larger then ushort");
            rawWriter.Write((ushort)selfBuf.Length);
            rawWriter.Write(selfBuf);

            rawWriter.Write((ushort)Clients.Count(x => x.Id != NewId));
            foreach (var client in Clients)
            {
                if (client.Id == NewId)
                    continue;
                rawWriter.Write(client.Id);
                rawWriter.Write(client.ParentId);
                var buf = EndPointSerializer.ToBytes(client.EndPoint);
                if (buf.Length > ushort.MaxValue) //-V3022
                    throw new DNMPException("buf.Length larger then ushort");
                rawWriter.Write((ushort)buf.Length);
                rawWriter.Write(buf);
            }

            var encryptedSymmetricKey = AsymmetricHelper.Encrypt(networkKey, SymmetricKey.GetBytes());

            writer.Write((ushort)encryptedSymmetricKey.Length);
            writer.Write(encryptedSymmetricKey);

            var rawData = rawNetworkInfoMemoryStream.ToArray();

            var encryptedNetworkInfo = SymmetricHelper.Encrypt(SymmetricKey, NetworkHashUtil.ComputeChecksum(rawData).Concat(rawData).ToArray());

            writer.Write(encryptedNetworkInfo.Length);
            writer.Write(encryptedNetworkInfo);

            return memoryStream.ToArray();
        }
    }
}
