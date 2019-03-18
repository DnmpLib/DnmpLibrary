using DnmpLibrary.Util;

namespace DnmpLibrary.Network
{
    internal class HashUtil
    {
        private static readonly Crc.Parameters hashParameters = Crc.Parameters.CRC32;
        
        private static readonly Crc crc = new Crc(hashParameters);

        public static byte[] ComputeChecksum(byte[] bytes) => crc.ComputeHash(bytes);

        public static int GetHashSize() => (hashParameters.HashSize + 7) / 8;
    }
}
