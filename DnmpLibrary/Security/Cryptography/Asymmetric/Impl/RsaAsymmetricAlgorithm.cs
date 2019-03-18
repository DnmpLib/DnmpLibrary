using System;
using System.Linq;
using System.Security.Cryptography;

namespace DnmpLibrary.Security.Cryptography.Asymmetric.Impl
{
    public class RsaAsymmetricAlgorithm : IAsymmetricAlgorithm
    {
        public byte[] Decrypt(IAsymmetricKey key, byte[] data)
        {
            if (!(key is RsaAsymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var rsaKey = (RsaAsymmetricKey)key;
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
            {
                rsaCryptoServiceProvider.ImportParameters(rsaKey.KeyParameters);
                return rsaCryptoServiceProvider.Decrypt(data, true);
            }
        }

        public byte[] Encrypt(IAsymmetricKey key, byte[] data)
        {
            if (!(key is RsaAsymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var rsaKey = (RsaAsymmetricKey)key;
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
            {
                rsaCryptoServiceProvider.ImportParameters(rsaKey.KeyParameters);
                return rsaCryptoServiceProvider.Encrypt(data, true);
            }
        }

        public byte[] Sign(IAsymmetricKey key, byte[] data)
        {
            if (!(key is RsaAsymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var rsaKey = (RsaAsymmetricKey)key;
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
            {
                rsaCryptoServiceProvider.ImportParameters(rsaKey.KeyParameters);
                return rsaCryptoServiceProvider.SignData(data, new SHA256CryptoServiceProvider());
            }
        }

        public bool Verify(IAsymmetricKey key, byte[] data, byte[] signature)
        {
            if (!(key is RsaAsymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var rsaKey = (RsaAsymmetricKey)key;
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
            {
                rsaCryptoServiceProvider.ImportParameters(rsaKey.KeyParameters);
                return rsaCryptoServiceProvider.VerifyData(data, new SHA256CryptoServiceProvider(), signature);
            }
        }
    }

    public class RsaAsymmetricKey : IAsymmetricKey
    {
        public RSAParameters KeyParameters;

        public RsaAsymmetricKey() { }

        public RsaAsymmetricKey(int size)
        {
            KeyParameters = new RSACryptoServiceProvider(size).ExportParameters(true);
        }

        public IAsymmetricAlgorithm GetAlgorithmInstance()
        {
            return new RsaAsymmetricAlgorithm();
        }

        public byte[] GetNetworkId() => KeyParameters.Modulus.Concat(KeyParameters.Exponent).ToArray();
    }
}
