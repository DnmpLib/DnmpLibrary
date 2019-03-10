using System.IO;
using DNMPLibrary.Security.Cryptography.Asymmetric;

namespace DNMPLibrary.Network.Messages.Types
{
    internal class SecondRankConnectionRequestMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.SecondRankConnectionRequest;
        
        public readonly byte[] SymmetricKeyBytes;
        private readonly IAsymmetricKey networkKey;
        
        public SecondRankConnectionRequestMessage(byte[] data, IAsymmetricKey networkKey)
        {
            this.networkKey = networkKey;

            var rawReader = new BinaryReader(new MemoryStream(data));

            SymmetricKeyBytes = AsymmetricHelper.Decrypt(this.networkKey, rawReader.ReadBytes(rawReader.ReadUInt16()));
        }

        public SecondRankConnectionRequestMessage(byte[] symmetricKey, IAsymmetricKey networkKey, bool packetCreation = false) //-V3117
        {
            SymmetricKeyBytes = symmetricKey;
            this.networkKey = networkKey;
        }

        public byte[] GetBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);

            var encryptedSymmetricKey = AsymmetricHelper.Encrypt(networkKey, SymmetricKeyBytes);

            writer.Write((ushort)encryptedSymmetricKey.Length);
            writer.Write(encryptedSymmetricKey);

            return memoryStream.ToArray();
        }
    }
}
