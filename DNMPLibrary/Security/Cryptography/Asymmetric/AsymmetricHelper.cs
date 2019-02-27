using System;
using System.Linq;
using System.Security.Cryptography;

namespace DNMPLibrary.Security.Cryptography.Asymmetric
{
    public static class AsymmetricHelper
    {
        public static byte[] Encrypt(IAsymmetricKey key, byte[] data)
        {
            return key.GetAlgorithmInstance().Encrypt(key, data);
        }

        public static byte[] Decrypt(IAsymmetricKey key, byte[] data)
        {
            return key.GetAlgorithmInstance().Decrypt(key, data);
        }

        public static byte[] Sign(IAsymmetricKey key, byte[] data)
        {
            return key.GetAlgorithmInstance().Sign(key, data);
        }

        public static bool Verify(IAsymmetricKey key, byte[] data, byte[] signature)
        {
            return key.GetAlgorithmInstance().Verify(key, data, signature);
        }
    }
}
