using System.IO;
using DNMPLibrary.Util.BigEndian;

namespace DNMPLibrary.Network.Messages.Types
{
    internal class PingUpdateReplyMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.PingUpdateReply;

        public PingPair[] UnDirectClientPings;

        public PingUpdateReplyMessage(byte[] data)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(data));
            UnDirectClientPings = new PingPair[reader.ReadUInt16()];
            for (var i = 0; i < UnDirectClientPings.Length; i++)
                UnDirectClientPings[i] = new PingPair
                {
                    Id = reader.ReadUInt16(),
                    Ping = reader.ReadUInt16()
                };
        }

        public PingUpdateReplyMessage(PingPair[] unDirectClientPings)
        {
            UnDirectClientPings = unDirectClientPings;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);

            writer.Write((ushort)UnDirectClientPings.Length);
            foreach (var pingPair in UnDirectClientPings)
            {
                writer.Write(pingPair.Id);
                writer.Write(pingPair.Ping);
            }

            return memoryStream.ToArray();
        }
    }

    public class PingPair
    {
        public ushort Id;
        public ushort Ping;
    }
}
