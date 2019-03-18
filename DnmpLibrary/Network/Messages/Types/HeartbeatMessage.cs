namespace DnmpLibrary.Network.Messages.Types
{
    internal class HeartbeatMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.Heartbeat;

        public byte[] GetBytes() => new byte[0];
    }
}
