using System.IO;
using DnmpLibrary.Util.BigEndian;

namespace DnmpLibrary.Network.Messages.Types
{
    internal class ParentSwitchMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ParentSwitch;

        public ushort ParentId;

        public ParentSwitchMessage(byte[] data)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(data));
            ParentId = reader.ReadUInt16();
        }

        public ParentSwitchMessage(ushort parentId)
        {
            ParentId = parentId;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);

            writer.Write(ParentId);

            return memoryStream.ToArray();
        }
    }
}
