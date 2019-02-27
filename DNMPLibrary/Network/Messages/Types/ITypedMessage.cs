namespace DNMPLibrary.Network.Messages.Types
{
    internal interface ITypedMessage
    {
        MessageType GetMessageType();

        byte[] GetBytes();
    }
}
