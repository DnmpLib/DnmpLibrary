namespace DnmpLibrary.Network.Messages.Types
{
    internal class SecondRankConnectionResponseMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.SecondRankConnectionResponse;

        public byte[] GetBytes() => new byte[0];
    }
}
