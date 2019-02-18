using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DNMPLibrary.Core;
using DNMPLibrary.Messages.Types;
using DNMPLibrary.Util;

namespace DNMPLibrary.Messages
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

        public long TotalLength => 1 + 2 + 2 + (MessageType.IsReliable() ? 16 : 0) + 4 + Payload.Length;

        public BaseMessage(byte[] messageData)
        {
            var reader = new BinaryReader(new MemoryStream(messageData));

            var messageInfo = reader.ReadByte();

            MessageFlags = (MessageFlags) (messageInfo >> 5);
            MessageType = (MessageType) (messageInfo & 0x1F);

            if (MessageFlags.HasFlag(MessageFlags.IsRedirected))
                RealSourceId = reader.ReadUInt16();

            SourceId = reader.ReadUInt16();
            DestinationId = reader.ReadUInt16();

            if (MessageType.IsReliable())
                Guid = new Guid(reader.ReadBytes(16));

            var payloadLength = reader.ReadInt32();
            if (payloadLength < 0)
                throw new DynNetException($"Payload length < 0: {payloadLength}");
            Payload = reader.ReadBytes(payloadLength);
        }

        public BaseMessage(ITypedMessage typedMessage, ushort sourceId, ushort destinationId, Guid id = new Guid())
        {
            Payload = typedMessage.GetBytes();
            MessageType = typedMessage.GetMessageType();
            Guid = id;
            SourceId = sourceId;
            DestinationId = destinationId;
            RealDestinationId = destinationId;
        }

        public BaseMessage(ITypedMessage typedMessage, ushort sourceId, ushort destinationId, ushort realSourceId, ushort realDestinationId, Guid id = new Guid())
        {
            Payload = typedMessage.GetBytes();
            MessageType = typedMessage.GetMessageType();
            Guid = id;
            SourceId = sourceId;
            DestinationId = destinationId;
            RealDestinationId = realDestinationId;
            RealSourceId = realSourceId;
            if (realDestinationId != destinationId)
                MessageFlags |= MessageFlags.IsRedirected;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);
            writer.Write((byte) ((int)MessageFlags << 5 | (int)MessageType));
            if (MessageFlags.HasFlag(MessageFlags.IsRedirected))
                writer.Write(RealSourceId);
            writer.Write(SourceId);
            writer.Write(DestinationId);
            if (MessageType.IsReliable())
                writer.Write(Guid.ToByteArray());
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

    public enum MessageType : byte
    {
        ConnectionRequest = 0x00,
        ConnectionRequestReply = 0x01,
        ConnectionRequestConfirm = 0x02,
        ConnectionRequestConfirmReply = 0x03,

        ConnectionNotification = 0x04,
        DisconnectionNotification = 0x05,
        PingUpdate = 0x06,
        PingUpdateReply = 0x07,
        Heartbeat = 0x08,

        ParentSwitch = 0x09,
        Ping = 0x10,
        Pong = 0x11,
        
        SecondRankConnectionRequest = 0x12,  
        SecondRankConnectionResponse = 0x13, 
        ReliableConfirm = 0x14,
        TryRestablishConnection = 0x15,
        TryRestablishConnectionReply = 0x16,
        Data = 0x17,
        DataBroadcast = 0x18,
        SubnetworkSpanningUpdate = 0x19,
        BroadcastedSubnetworkSpanningUpdate = 0x1A,
        SubnetworkSpanningUpdateReply = 0x1B
    }

    public static class MessageTypeExtensions
    {
        public static bool OnlyBroadcasted(this MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.ConnectionRequest:
                case MessageType.ConnectionRequestReply:
                case MessageType.ConnectionRequestConfirm:
                case MessageType.ConnectionRequestConfirmReply:
                case MessageType.PingUpdate:
                case MessageType.PingUpdateReply:
                case MessageType.Heartbeat:
                case MessageType.Ping:
                case MessageType.Pong:
                case MessageType.SecondRankConnectionRequest:
                case MessageType.SecondRankConnectionResponse:
                case MessageType.ReliableConfirm:
                case MessageType.TryRestablishConnection:
                case MessageType.TryRestablishConnectionReply:
                case MessageType.Data:
                case MessageType.SubnetworkSpanningUpdate:
                case MessageType.SubnetworkSpanningUpdateReply:
                    return false;
                case MessageType.ConnectionNotification:
                case MessageType.DisconnectionNotification:
                case MessageType.ParentSwitch:
                case MessageType.DataBroadcast:
                case MessageType.BroadcastedSubnetworkSpanningUpdate:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsReliable(this MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.ConnectionRequest:
                case MessageType.ConnectionRequestReply:
                case MessageType.ConnectionRequestConfirm:
                case MessageType.Heartbeat:
                case MessageType.Ping:
                case MessageType.Pong:
                case MessageType.ReliableConfirm:
                case MessageType.Data:
                    return false;
                case MessageType.TryRestablishConnection:
                case MessageType.TryRestablishConnectionReply:
                case MessageType.SecondRankConnectionRequest:
                case MessageType.SecondRankConnectionResponse:
                case MessageType.PingUpdate:
                case MessageType.PingUpdateReply:
                case MessageType.ConnectionRequestConfirmReply:
                case MessageType.ConnectionNotification:
                case MessageType.DisconnectionNotification:
                case MessageType.ParentSwitch:
                case MessageType.DataBroadcast:
                case MessageType.SubnetworkSpanningUpdate:
                case MessageType.BroadcastedSubnetworkSpanningUpdate:
                case MessageType.SubnetworkSpanningUpdateReply:
                    return true;
                default:
                    return false;
            }
        }

        public static bool ShouldBeEncrypted(this MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.ConnectionRequest:
                case MessageType.ConnectionRequestReply:
                case MessageType.ConnectionRequestConfirm:
                case MessageType.ConnectionRequestConfirmReply:
                case MessageType.SecondRankConnectionRequest:
                case MessageType.Ping:
                case MessageType.Pong:
                case MessageType.ReliableConfirm:
                    return false;
                case MessageType.SecondRankConnectionResponse:
                case MessageType.Heartbeat:
                case MessageType.Data:
                case MessageType.TryRestablishConnection:
                case MessageType.TryRestablishConnectionReply:
                case MessageType.PingUpdate:
                case MessageType.PingUpdateReply:
                case MessageType.ConnectionNotification:
                case MessageType.DisconnectionNotification:
                case MessageType.ParentSwitch:
                case MessageType.DataBroadcast:
                case MessageType.SubnetworkSpanningUpdate:
                case MessageType.BroadcastedSubnetworkSpanningUpdate:
                case MessageType.SubnetworkSpanningUpdateReply:
                    return true;
                default:
                    return false;
            }
        }
    }
}
