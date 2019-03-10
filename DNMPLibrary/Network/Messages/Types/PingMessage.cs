namespace DNMPLibrary.Network.Messages.Types
{
    internal class PingMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.Ping;

        public byte[] Payload;

        public PingMessage(byte[] data)
        {
            Payload = data;
        }

        public byte[] GetBytes()
        {
            return Payload;
        }
    }
}
