using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace DNMPLibrary.Security
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

    public interface IAsymmetricAlgorithm
    {
        byte[] Encrypt(IAsymmetricKey key, byte[] data);
        byte[] Decrypt(IAsymmetricKey key, byte[] data);
        byte[] Sign(IAsymmetricKey key, byte[] data);
        bool Verify(IAsymmetricKey key, byte[] data, byte[] signature);
    }

    public class RSAAsymmetricAlgorithm : IAsymmetricAlgorithm
    {
        public byte[] Decrypt(IAsymmetricKey key, byte[] data)
        {
            if (!(key is RSAAsymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var rsaKey = (RSAAsymmetricKey) key;
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
            var rsaKey = (RSAAsymmetricKey) key;
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
            var rsaKey = (RSAAsymmetricKey) key;
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
            var rsaKey = (RSAAsymmetricKey) key;
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
            {
                rsaCryptoServiceProvider.ImportParameters(rsaKey.KeyParameters);
                return rsaCryptoServiceProvider.VerifyData(data, new SHA256CryptoServiceProvider(), signature);
            }
        }
    }

    public interface IAsymmetricKey
    {
        IAsymmetricAlgorithm GetAlgorithmInstance();

        byte[] GetNetworkId();
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
