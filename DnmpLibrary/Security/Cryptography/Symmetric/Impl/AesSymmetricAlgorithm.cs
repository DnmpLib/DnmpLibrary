using System;
using System.IO;
using System.Security.Cryptography;
using DnmpLibrary.Util.BigEndian;

namespace DnmpLibrary.Security.Cryptography.Symmetric.Impl
{
    public class AesSymmetricAlgorithm : ISymmetricAlgorithm
    {
        public byte[] Decrypt(ISymmetricKey key, byte[] data)
        {
            if (!(key is AesSymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var aesKey = (AesSymmetricKey)key;

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
                        using (var binaryReader = new BigEndianBinaryReader(cryptoStream))
                        {
                            return binaryReader.ReadBytes(binaryReader.ReadInt32());
                        }
                    }
                }
            }
        }

        public byte[] Encrypt(ISymmetricKey key, byte[] data)
        {
            if (!(key is AesSymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var aesKey = (AesSymmetricKey)key;

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
                        using (var binaryWriter = new BigEndianBinaryWriter(cryptoStream))
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

    public class AesSymmetricKey : ISymmetricKey
    {
        public ISymmetricKey CreateFromBytes(byte[] data) => new AesSymmetricKey(data);

        public ISymmetricKey GenerateNewKey() => new AesSymmetricKey();

        public ISymmetricAlgorithm GetAlgorithmInstance() => new AesSymmetricAlgorithm();

        public byte[] GetBytes() => Key;

        public byte[] Key = new byte[16];

        public AesSymmetricKey()
        {
            RandomNumberGenerator.Create().GetNonZeroBytes(Key);
        }

        public AesSymmetricKey(byte[] data)
        {
            Key = data;
        }
    }
}
