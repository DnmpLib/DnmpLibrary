using System;
using System.IO;
using System.Security.Cryptography;
using DNMPLibrary.Util.BigEndian;

namespace DNMPLibrary.Security.Cryptography.Symmetric.Impl
{
    public class AESSymmetricAlgorithm : ISymmetricAlgorithm
    {
        public byte[] Decrypt(ISymmetricKey key, byte[] data)
        {
            if (!(key is AESSymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var aesKey = (AESSymmetricKey)key;

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
            if (!(key is AESSymmetricKey))
                throw new ArgumentException("Wrong key type", nameof(key));
            var aesKey = (AESSymmetricKey)key;

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
