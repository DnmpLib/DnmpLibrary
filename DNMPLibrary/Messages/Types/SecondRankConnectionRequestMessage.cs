using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DNMPLibrary.Security;
using DNMPLibrary.Client;
using DNMPLibrary.Core;

namespace DNMPLibrary.Messages.Types
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
