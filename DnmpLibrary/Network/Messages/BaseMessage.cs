using System;
using System.IO;
using DnmpLibrary.Core;
using DnmpLibrary.Network.Messages.Types;
using DnmpLibrary.Util.BigEndian;

namespace DnmpLibrary.Network.Messages
{
    internal class BaseMessage
    {
        public MessageFlags MessageFlags { get; set; }

        public MessageType MessageType { get; }

        public ushort SourceId { get; }

        public ushort RealSourceId { get; set; } = 0xFFFF;

        public ushort DestinationId { get; }

        public ushort RealDestinationId { get; set; }

        public Guid Guid { get; }

        public byte[] ReceivedHash { get; set;  }

        public byte[] Payload { get; set; }

        public byte[] Hash { get; set; }

        public byte[] RealHash { get; }

        public long TotalLength => 1 + 2 + 2 + (MessageType.IsReliable() ? 16 : 0) + 4 + Payload.Length;

        public byte[] SecurityHash
        {
            get
            {
                var hashMemoryStream = new MemoryStream();
                var hashBinaryWriter = new BigEndianBinaryWriter(hashMemoryStream);
                hashBinaryWriter.Write((byte) MessageFlags);
                hashBinaryWriter.Write((byte) MessageType);
                if (MessageFlags.HasFlag(MessageFlags.IsRedirected))
                    hashBinaryWriter.Write(RealSourceId);
                hashBinaryWriter.Write(SourceId);
                hashBinaryWriter.Write(DestinationId);
                if (MessageType.IsReliable())
                    hashBinaryWriter.Write(Guid.ToByteArray());
                hashBinaryWriter.Write(Payload);
                return HashUtil.ComputeChecksum(hashMemoryStream.ToArray());
            }
        }

        public BaseMessage(byte[] messageData)
        {
            var reader = new BigEndianBinaryReader(new MemoryStream(messageData));

            var messageInfo = reader.ReadByte();

            MessageFlags = (MessageFlags) (messageInfo >> 5);
            MessageType = (MessageType) (messageInfo & 0x1F);

            if (MessageFlags.HasFlag(MessageFlags.IsRedirected))
                RealSourceId = reader.ReadUInt16();

            SourceId = reader.ReadUInt16();
            DestinationId = reader.ReadUInt16();

            if (MessageType.IsReliable())
            {
                Guid = new Guid(reader.ReadBytes(16));
                Hash = reader.ReadBytes(HashUtil.GetHashSize());
            }

            var payloadLength = reader.ReadInt32();
            if (payloadLength < 0)
                throw new DnmpException($"Payload length < 0: {payloadLength}");
            Payload = reader.ReadBytes(payloadLength);
            RealHash = HashUtil.ComputeChecksum(Payload);
        }

        public BaseMessage(ITypedMessage typedMessage, ushort sourceId, ushort destinationId, Guid id = new Guid()) :
            this(typedMessage, sourceId, destinationId, sourceId, destinationId, id) {  }

        public BaseMessage(ITypedMessage typedMessage, ushort sourceId, ushort destinationId, ushort realSourceId, ushort realDestinationId, Guid id = new Guid()) :
            this(typedMessage.GetBytes(), typedMessage.GetMessageType(), sourceId, destinationId, realSourceId, realDestinationId, id) {  }

        public BaseMessage(byte[] payload, MessageType messageType, ushort sourceId, ushort destinationId, ushort realSourceId, ushort realDestinationId, Guid id = new Guid())
        {
            Payload = payload;
            MessageType = messageType;
            Guid = id;
            SourceId = sourceId;
            DestinationId = destinationId;
            RealDestinationId = realDestinationId;
            RealSourceId = realSourceId;
            if (realDestinationId != destinationId)
                MessageFlags |= MessageFlags.IsRedirected;
            RealHash = HashUtil.ComputeChecksum(Payload);
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BigEndianBinaryWriter(memoryStream);
            writer.Write((byte) ((int)MessageFlags << 5 | (int)MessageType));
            if (MessageFlags.HasFlag(MessageFlags.IsRedirected))
                writer.Write(RealSourceId);
            writer.Write(SourceId);
            writer.Write(DestinationId);
            if (MessageType.IsReliable())
            {
                writer.Write(Guid.ToByteArray());
                writer.Write(RealHash);
            }

            writer.Write(Payload.Length);
            writer.Write(Payload);
            return memoryStream.ToArray();
        }
    }

    [Flags]
    public enum MessageFlags : byte
    {
        IsRedirected = 1
    }
}
