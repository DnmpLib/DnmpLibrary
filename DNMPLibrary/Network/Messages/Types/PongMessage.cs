namespace DNMPLibrary.Network.Messages.Types
{
    internal class PongMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.Pong;

        public byte[] Payload;

        public PongMessage(byte[] data)
        {
            Payload = data;
        }

        public byte[] GetBytes()
        {
            return Payload;
        }
    }
}
