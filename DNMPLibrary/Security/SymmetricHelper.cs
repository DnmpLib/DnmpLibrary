using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Security
{
    public static class SymmetricHelper
    {
        public static byte[] Encrypt(ISymmetricKey key, byte[] data)
        {
            return key.GetAlgorithmInstance().Encrypt(key, data);
        }

        public static byte[] Decrypt(ISymmetricKey key, byte[] data)
        {
            return key.GetAlgorithmInstance().Decrypt(key, data);
        }
    }

    public interface ISymmetricAlgorithm
    {
        byte[] Encrypt(ISymmetricKey key, byte[] data);
        byte[] Decrypt(ISymmetricKey key, byte[] data);
    }

    public class PlainSymmetricAlgorithm : ISymmetricAlgorithm
    {
        public byte[] Decrypt(ISymmetricKey key, byte[] data)
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

    public class AESSymmetricAlgorithm : ISymmetricAlgorithm
    {
        public byte[] Decrypt(ISymmetricKey key, byte[] data)
        {
            if (!(key is AESSymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var aesKey = (AESSymmetricKey) key;

            using (var rijndaelManaged = new RijndaelManaged())
            {
                rijndaelManaged.KeySize = 128;
                rijndaelManaged.Key = aesKey.Key;
                rijndaelManaged.GenerateIV();
                using (var memoryStream = new MemoryStream(data))
                {
                    var iv = new byte[16];
                    memoryStream.Read(iv, 0, 16);
                    rijndaelManaged.IV = iv;
                    var decryptor = rijndaelManaged.CreateDecryptor(rijndaelManaged.Key, rijndaelManaged.IV);
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (var binaryReader = new BinaryReader(cryptoStream))
                        {
                            return binaryReader.ReadBytes(binaryReader.ReadInt32());
                        }
                    }
                }
            }
        }

        public byte[] Encrypt(ISymmetricKey key, byte[] data)
        {
            if (!(key is AESSymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var aesKey = (AESSymmetricKey) key;

            using (var rijndaelManaged = new RijndaelManaged())
            {
                rijndaelManaged.KeySize = 128;
                rijndaelManaged.Key = aesKey.Key;
                rijndaelManaged.GenerateIV();
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(rijndaelManaged.IV, 0, 16);
                    var encryptor = rijndaelManaged.CreateEncryptor(rijndaelManaged.Key, rijndaelManaged.IV);
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (var binaryWriter = new BinaryWriter(cryptoStream))
                        {
                            binaryWriter.Write(data.Length);
                            binaryWriter.Write(data);
                        }

                        return memoryStream.ToArray();
                    }
                }
            }
        }
    }

    public interface ISymmetricKey
    {
        ISymmetricAlgorithm GetAlgorithmInstance();

        byte[] GetBytes();

        ISymmetricKey CreateFromBytes(byte[] data);

        ISymmetricKey GenerateNewKey();
    }

    public class PlainSymmetricKey : ISymmetricKey
    {
        public ISymmetricKey CreateFromBytes(byte[] data) => new PlainSymmetricKey();

        public ISymmetricKey GenerateNewKey() => new PlainSymmetricKey();

        public ISymmetricAlgorithm GetAlgorithmInstance() => new PlainSymmetricAlgorithm();

        public byte[] GetBytes() => new byte[0];
    }

    public class AESSymmetricKey : ISymmetricKey
    {
        public ISymmetricKey CreateFromBytes(byte[] data) => new AESSymmetricKey(data);

        public ISymmetricKey GenerateNewKey() => new AESSymmetricKey();

        public ISymmetricAlgorithm GetAlgorithmInstance() => new AESSymmetricAlgorithm();

        public byte[] GetBytes() => Key;

        public byte[] Key = new byte[16];

        public AESSymmetricKey()
        {
            RandomNumberGenerator.Create().GetNonZeroBytes(Key);
        }

        public AESSymmetricKey(byte[] data)
        {
            Key = data;
        }
    }
}
