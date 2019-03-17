using System.IO;
using DNMPLibrary.Core;
using DNMPLibrary.Interaction.Protocol;
using DNMPLibrary.Util.BigEndian;

namespace DNMPLibrary.Network.Messages.Types
{
    internal class ConnectionNotificationMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionNotification;

        public readonly ushort Id;
        public readonly IEndPoint EndPoint;

        private readonly IEndPointFactory endPointFactory;

        public ConnectionNotificationMessage(byte[] data, IEndPointFactory endPointFactory)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(data));

            Id = reader.ReadUInt16();
            EndPoint = endPointFactory.DeserializeEndPoint(reader.ReadBytes(reader.ReadUInt16()));
        }

        public ConnectionNotificationMessage(ushort id, IEndPoint endPoint, IEndPointFactory endPointFactory)
        {
            Id = id;
            EndPoint = endPoint;
            this.endPointFactory = endPointFactory;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);
            
            writer.Write(Id);
            var buf = endPointFactory.SerializeEndPoint(EndPoint);
            if (buf.Length > ushort.MaxValue) //-V3022
                throw new DNMPException("buf.Length larger then ushort");
            writer.Write((ushort)buf.Length);
            writer.Write(buf);

            return memoryStream.ToArray();
        }
    }
}
