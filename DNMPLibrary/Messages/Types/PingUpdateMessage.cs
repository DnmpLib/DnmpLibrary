using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DNMPLibrary.Client;
using DNMPLibrary.Core;
using DNMPLibrary.Util;

namespace DNMPLibrary.Messages.Types
{
    internal class PingUpdateMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.PingUpdate;

        public ushort[] UnDirectClientIds;

        public PingUpdateMessage(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));
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
            var writer = new BinaryWriter(memoryStream);

            writer.Write((ushort) UnDirectClientIds.Length);
            foreach (var id in UnDirectClientIds)
                writer.Write(id);

            return memoryStream.ToArray();
        }
    }
}
