namespace DNMPLibrary.Network.Messages.Types
{
    internal class DummyMessage : ITypedMessage
    {
        private readonly BaseMessage message;

        public DummyMessage(BaseMessage message)
        {
            this.message = message;
        }

        public MessageType GetMessageType() => message.MessageType;

        public byte[] GetBytes() => message.Payload;
    }
}
