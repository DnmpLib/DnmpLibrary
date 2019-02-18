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
    internal class PingUpdateReplyMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.PingUpdateReply;

        public PingPair[] UnDirectClientPings;

        public PingUpdateReplyMessage(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));
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
            var writer = new BinaryWriter(memoryStream);

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
