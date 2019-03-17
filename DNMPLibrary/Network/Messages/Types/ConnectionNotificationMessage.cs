using System.IO;
using DNMPLibrary.Core;
using DNMPLibrary.Interaction.Protocol;
using DNMPLibrary.Util.BigEndian;

namespace DNMPLibrary.Network.Messages.Types
{
    internal class ConnectionNotificationMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionNotification;

        public readonly DNMPNode Client;

        private readonly IEndPointFactory endPointFactory;

        public ConnectionNotificationMessage(byte[] data, IEndPointFactory endPointFactory)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(data));
            Client = new DNMPNode
            {
                Id = reader.ReadUInt16(),
                EndPoint = endPointFactory.DeserializeEndPoint(reader.ReadBytes(reader.ReadUInt16())),
                CustomData = reader.ReadBytes(reader.ReadUInt16())
            };
        }

        public ConnectionNotificationMessage(DNMPNode client, IEndPointFactory endPointFactory)
        {
            Client = client;
            this.endPointFactory = endPointFactory;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);
            
            writer.Write(Client.Id);
            var buf = endPointFactory.SerializeEndPoint(Client.EndPoint);
            if (buf.Length > ushort.MaxValue) 
                throw new DNMPException("buf.Length larger then ushort");
            writer.Write((ushort)buf.Length);
            writer.Write(buf);
            writer.Write((ushort)Client.CustomData.Length);
            writer.Write(Client.CustomData);

            return memoryStream.ToArray();
        }
    }
}
