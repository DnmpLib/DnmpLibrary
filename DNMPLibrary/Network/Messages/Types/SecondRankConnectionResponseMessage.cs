namespace DNMPLibrary.Network.Messages.Types
{
    internal class SecondRankConnectionResponseMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.SecondRankConnectionResponse;

        public SecondRankConnectionResponseMessage() { }

        public byte[] GetBytes() => new byte[0];
    }
}
