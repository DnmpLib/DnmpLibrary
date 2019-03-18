namespace DnmpLibrary.Network.Messages
{

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
                case MessageType.ConnectionRequestConfirm:
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
                case MessageType.SecondRankConnectionRequest:
                case MessageType.Ping:
                case MessageType.Pong:
                case MessageType.ReliableConfirm:
                    return false;
                case MessageType.ConnectionRequestConfirmReply:
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
