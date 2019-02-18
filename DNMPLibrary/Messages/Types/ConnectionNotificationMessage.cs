using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DNMPLibrary.Core;
using DNMPLibrary.Util;
using DNMPLibrary.Client;

namespace DNMPLibrary.Messages.Types
{
    internal class ConnectionNotificationMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionNotification;

        public readonly ushort Id;
        public readonly EndPoint EndPoint;

        public ConnectionNotificationMessage(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));

            Id = reader.ReadUInt16();
            EndPoint = EndPointSerializer.FromBytes(reader.ReadBytes(reader.ReadUInt16()));
        }

        public ConnectionNotificationMessage(ushort id, EndPoint endPoint)
        {
            Id = id;
            EndPoint = endPoint;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);
            
            writer.Write(Id);
            var buf = EndPointSerializer.ToBytes(EndPoint);
            if (buf.Length > ushort.MaxValue) //-V3022
                throw new DNMPException("buf.Length larger then ushort");
            writer.Write((ushort)buf.Length);
            writer.Write(buf);

            return memoryStream.ToArray();
        }
    }
}
