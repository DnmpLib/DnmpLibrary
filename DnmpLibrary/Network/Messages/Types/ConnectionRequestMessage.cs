namespace DnmpLibrary.Network.Messages.Types
{
    internal class ConnectionRequestMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionRequest;

        public byte[] NetworkId;

        public ConnectionRequestMessage(byte[] data)
        {
            NetworkId = data;
        }

        // ReSharper disable once UnusedParameter.Local
        public ConnectionRequestMessage(byte[] networkId, bool packetCreation = false) //-V3117
        {
            NetworkId = networkId;
        }

        public byte[] GetBytes() => NetworkId;
    }
}
