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
    internal class ParentSwitchMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ParentSwitch;

        public ushort ParentId;

        public ParentSwitchMessage(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));
            ParentId = reader.ReadUInt16();
        }

        public ParentSwitchMessage(ushort parentId)
        {
            ParentId = parentId;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);

            writer.Write(ParentId);

            return memoryStream.ToArray();
        }
    }
}
