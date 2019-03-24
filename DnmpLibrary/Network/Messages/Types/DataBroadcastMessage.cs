namespace DnmpLibrary.Network.Messages.Types
{
    internal class DataBroadcastMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.DataBroadcast;

        public byte[] Payload;

        public DataBroadcastMessage(byte[] data)
        {
            Payload = data;
        }

        public DataBroadcastMessage(byte[] data, bool packetCreation = false) //-V3117
        {
            Payload = data;
        }

        public byte[] GetBytes()
        {
            return Payload;
        }
    }
}
