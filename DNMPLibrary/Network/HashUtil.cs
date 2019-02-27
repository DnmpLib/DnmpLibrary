using DNMPLibrary.Util;

namespace DNMPLibrary.Network
{
    internal class HashUtil
    {
        private static readonly CRC.Parameters hashParameters = CRC.Parameters.CRC32;
        
        private static readonly CRC crc = new CRC(hashParameters);

        public static byte[] ComputeChecksum(byte[] bytes) => crc.ComputeHash(bytes);

        public static int GetHashSize() => (hashParameters.HashSize + 7) / 8;
    }
}
