using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DNMPLibrary.Core;
using DNMPLibrary.Client;
using DNMPLibrary.Util;

namespace DNMPLibrary.Messages.Types
{
    internal class DisconnectionNotificationMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.DisconnectionNotification;

        public readonly ushort Id;

        public DisconnectionNotificationMessage(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));

            Id = reader.ReadUInt16();
        }

        public DisconnectionNotificationMessage(ushort id)
        {
            Id = id;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);

            writer.Write(Id);

            return memoryStream.ToArray();
        }
    }
}
