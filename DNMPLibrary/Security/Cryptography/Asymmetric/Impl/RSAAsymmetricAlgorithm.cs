using System;
using System.Linq;
using System.Security.Cryptography;

namespace DNMPLibrary.Security.Cryptography.Asymmetric.Impl
{
    public class RSAAsymmetricAlgorithm : IAsymmetricAlgorithm
    {
        public byte[] Decrypt(IAsymmetricKey key, byte[] data)
        {
            if (!(key is RSAAsymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var rsaKey = (RSAAsymmetricKey)key;
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
            {
                rsaCryptoServiceProvider.ImportParameters(rsaKey.KeyParameters);
                return rsaCryptoServiceProvider.Decrypt(data, true);
            }
        }

        public byte[] Encrypt(IAsymmetricKey key, byte[] data)
        {
            if (!(key is RSAAsymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var rsaKey = (RSAAsymmetricKey)key;
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
            {
                rsaCryptoServiceProvider.ImportParameters(rsaKey.KeyParameters);
                return rsaCryptoServiceProvider.Encrypt(data, true);
            }
        }

        public byte[] Sign(IAsymmetricKey key, byte[] data)
        {
            if (!(key is RSAAsymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var rsaKey = (RSAAsymmetricKey)key;
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
            {
                rsaCryptoServiceProvider.ImportParameters(rsaKey.KeyParameters);
                return rsaCryptoServiceProvider.SignData(data, new SHA256CryptoServiceProvider());
            }
        }

        public bool Verify(IAsymmetricKey key, byte[] data, byte[] signature)
        {
            if (!(key is RSAAsymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var rsaKey = (RSAAsymmetricKey)key;
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
            {
                rsaCryptoServiceProvider.ImportParameters(rsaKey.KeyParameters);
                return rsaCryptoServiceProvider.VerifyData(data, new SHA256CryptoServiceProvider(), signature);
            }
        }
    }

    public class RSAAsymmetricKey : IAsymmetricKey
    {
        public RSAParameters KeyParameters;

        public RSAAsymmetricKey() { }

        public RSAAsymmetricKey(int size)
        {
            KeyParameters = new RSACryptoServiceProvider(size).ExportParameters(true);
        }

        public IAsymmetricAlgorithm GetAlgorithmInstance()
        {
            return new RSAAsymmetricAlgorithm();
        }

        public byte[] GetNetworkId() => KeyParameters.Modulus.Concat(KeyParameters.Exponent).ToArray();
    }
}
