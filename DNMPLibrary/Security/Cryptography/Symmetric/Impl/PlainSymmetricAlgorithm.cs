using System;

namespace DNMPLibrary.Security.Cryptography.Symmetric.Impl
{
    public class PlainSymmetricAlgorithm : ISymmetricAlgorithm
    {
        public byte[] Decrypt(ISymmetricKey key, byte[] data) //-V3013
        {
            if (!(key is PlainSymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            return data;
        }

        public byte[] Encrypt(ISymmetricKey key, byte[] data)
        {
            if (!(key is PlainSymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            return data;
        }
    }

    public class PlainSymmetricKey : ISymmetricKey
    {
        public ISymmetricKey CreateFromBytes(byte[] data) => new PlainSymmetricKey();

        public ISymmetricKey GenerateNewKey() => new PlainSymmetricKey();

        public ISymmetricAlgorithm GetAlgorithmInstance() => new PlainSymmetricAlgorithm();

        public byte[] GetBytes() => new byte[0];
    }
}
