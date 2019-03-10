using System;
using System.Linq;

namespace DNMPLibrary.Util
{
    internal class CRC
    {
        public struct Parameters
        {
            internal readonly ulong Poly;
            internal readonly int HashSize;
            internal readonly ulong Init;
            internal readonly bool RefIn;
            internal readonly bool RefOut;
            internal readonly ulong XorOut;

            public Parameters(ulong poly, int hashSize, ulong init, bool refIn, bool refOut, ulong xorOut)
            {
                Poly = poly;
                HashSize = hashSize;
                Init = init;
                RefIn = refIn;
                RefOut = refOut;
                XorOut = xorOut;
            }

            #region PRESETS
            public static Parameters CRC3Rohc => new Parameters(0x3, 3, 0x7, true, true, 0x0);
            public static Parameters CRC4Itu => new Parameters(0x3, 4, 0, true, true, 0x0);
            public static Parameters CRC5Epc => new Parameters(0x9, 5, 0x9, false, false, 0x0);
            public static Parameters CRC5Itu => new Parameters(0x15, 5, 0x0, true, true, 0x0);
            public static Parameters CRC5Usb => new Parameters(0x5, 5, 0x1F, true, true, 0x1F);
            public static Parameters CRC6Cdma2000A => new Parameters(0x27, 6, 0x3F, false, false, 0x0);
            public static Parameters CRC6Cdma2000B => new Parameters(0x7, 6, 0x3F, false, false, 0x0);
            public static Parameters CRC6Darc => new Parameters(0x19, 6, 0x0, true, true, 0x0);
            public static Parameters CRC6Itu => new Parameters(0x3, 6, 0x0, true, true, 0x0);
            public static Parameters CRC7 => new Parameters(0x9, 7, 0x0, false, false, 0x0);
            public static Parameters CRC7Rohc => new Parameters(0x4F, 7, 0x7F, true, true, 0x0);
            public static Parameters CRC8 => new Parameters(0x7, 8, 0x0, false, false, 0x0);
            public static Parameters CRC8Cdma2000 => new Parameters(0x9B, 8, 0xFF, false, false, 0x0);
            public static Parameters CRC8Darc => new Parameters(0x39, 8, 0x0, true, true, 0x0);
            public static Parameters CRC8DvbS2 => new Parameters(0xD5, 8, 0x0, false, false, 0x0);
            public static Parameters CRC8Ebu => new Parameters(0x1D, 8, 0xFF, true, true, 0x0);
            public static Parameters CRC8ICode => new Parameters(0x1D, 8, 0xFD, false, false, 0x0);
            public static Parameters CRC8Itu => new Parameters(0x7, 8, 0x0, false, false, 0x55);
            public static Parameters CRC8Maxim => new Parameters(0x31, 8, 0x0, true, true, 0x0);
            public static Parameters CRC8Rohc => new Parameters(0x7, 8, 0xFF, true, true, 0x00);
            public static Parameters CRC8Wcdma => new Parameters(0x9B, 8, 0x0, true, true, 0x0);
            public static Parameters CRC10 => new Parameters(0x233, 10, 0x0, false, false, 0x0);
            public static Parameters CRC10Cdma2000 => new Parameters(0x3D9, 10, 0x3FF, false, false, 0x0);
            public static Parameters CRC11 => new Parameters(0x385, 11, 0x1A, false, false, 0x0);
            public static Parameters CRC123Gpp => new Parameters(0x80F, 12, 0x0, false, true, 0x0);
            public static Parameters CRC12Cdma2000 => new Parameters(0x3D9, 12, 0xFFF, false, false, 0x0);
            public static Parameters CRC12Dect => new Parameters(0x80F, 12, 0x0, false, false, 0x0);
            public static Parameters CRC13Bbc => new Parameters(0x1CF5, 13, 0x0, false, false, 0x0);
            public static Parameters CRC14Darc => new Parameters(0x805, 13, 0x0, true, true, 0x0);
            public static Parameters CRC15 => new Parameters(0x4599, 15, 0x0, false, false, 0x0);
            public static Parameters CRC15Mpt1327 => new Parameters(0x6815, 15, 0x0, false, false, 0x1);
            public static Parameters CRC16Arc => new Parameters(0x8005, 16, 0x0, true, true, 0x0);
            public static Parameters CRC16AugCcitt => new Parameters(0x1021, 16, 0x1D0F, false, false, 0x0);
            public static Parameters CRC16Buypass => new Parameters(0x8005, 16, 0x0, false, false, 0x0);
            public static Parameters CRC16CcittFalse => new Parameters(0x1021, 16, 0xFFFF, false, false, 0x0);
            public static Parameters CRC16Cdma2000 => new Parameters(0xC867, 16, 0xFFFF, false, false, 0x0);
            public static Parameters CRC16Dds110 => new Parameters(0x8006, 16, 0x800D, false, false, 0x0);
            public static Parameters CRC16DectR => new Parameters(0x589, 16, 0x0, false, false, 0x1);
            public static Parameters CRC16DectX => new Parameters(0x589, 16, 0x0, false, false, 0x0);
            public static Parameters CRC16Dnp => new Parameters(0x3D65, 16, 0x0, true, true, 0xFFFF);
            public static Parameters CRC16En13757 => new Parameters(0x3D65, 16, 0x0, false, false, 0xFFFF);
            public static Parameters CRC16Genibus => new Parameters(0x1021, 16, 0xFFFF, false, false, 0xFFFF);
            public static Parameters CRC16Maxim => new Parameters(0x8005, 16, 0x0, true, true, 0xFFFF);
            public static Parameters CRC16Mcrf4XX => new Parameters(0x1021, 16, 0xFFFF, true, true, 0x0);
            public static Parameters CRC16Riello => new Parameters(0x1021, 16, 0xB2AA, true, true, 0x0);
            public static Parameters CRC16T10Diff => new Parameters(0x8BB7, 16, 0x0, false, false, 0x0);
            public static Parameters CRC16Teledisk => new Parameters(0xA097, 16, 0x0, false, false, 0x0);
            public static Parameters CRC16Tms37157 => new Parameters(0x1021, 16, 0x89EC, true, true, 0x0);
            public static Parameters CRC16Usb => new Parameters(0x8005, 16, 0xFFFF, true, true, 0xFFFF);
            public static Parameters CRCA => new Parameters(0x1021, 16, 0xC6C6, true, true, 0x0);
            public static Parameters CRC16Kermit => new Parameters(0x1021, 16, 0x0, true, true, 0x0);
            public static Parameters CRC16Modbus => new Parameters(0x8005, 16, 0xFFFF, true, true, 0x0);
            public static Parameters CRC16X25 => new Parameters(0x1021, 16, 0xFFFF, true, true, 0xFFFF);
            public static Parameters CRC16Xmodem => new Parameters(0x1021, 16, 0x0, false, false, 0x0);
            public static Parameters CRC24 => new Parameters(0x864CFB, 24, 0xB704CE, false, false, 0x0);
            public static Parameters CRC24FlexrayA => new Parameters(0x5D6DCB, 24, 0xFEDCBA, false, false, 0x0);
            public static Parameters CRC24FlexrayB => new Parameters(0x5D6DCB, 24, 0xABCDEF, false, false, 0x0);
            public static Parameters CRC32Philips => new Parameters(0x4C11DB7, 31, 0x7FFFFFFF, false, false, 0x7FFFFFFF);
            public static Parameters CRC32 => new Parameters(0x04C11DB7, 32, 0xFFFFFFFF, true, true, 0xFFFFFFFF);
            public static Parameters CRC32Bzip2 => new Parameters(0x04C11DB7, 32, 0xFFFFFFFF, false, false, 0xFFFFFFFF);
            public static Parameters CRC32C => new Parameters(0x1EDC6F41, 32, 0xFFFFFFFF, true, true, 0xFFFFFFFF);
            public static Parameters CRC32D => new Parameters(0xA833982B, 32, 0xFFFFFFFF, true, true, 0xFFFFFFFF);
            public static Parameters CRC32Mpeg2 => new Parameters(0x04C11DB7, 32, 0xFFFFFFFF, false, false, 0x0);
            public static Parameters CRC32Posix => new Parameters(0x04C11DB7, 32, 0x0, false, false, 0xFFFFFFFF);
            public static Parameters CRC32Q => new Parameters(0x814141AB, 32, 0x0, false, false, 0x0);
            public static Parameters CRC32Jamcrc => new Parameters(0x04C11DB7, 32, 0xFFFFFFFF, true, true, 0x0);
            public static Parameters CRC32Xfer => new Parameters(0x000000AF, 32, 0x0, false, false, 0x0);
            public static Parameters CRC32Zlib => new Parameters(0x4C11DB7, 32, 0xFFFFFFFF, true, true, 0xFFFFFFFF);
            public static Parameters CRC40Gsm => new Parameters(0x4820009, 40, 0x0, false, false, 0xFFFFFFFFFF);
            public static Parameters CRC64 => new Parameters(0x42F0E1EBA9EA3693, 64, 0x0, false, false, 0x0);
            public static Parameters CRC64We => new Parameters(0x42F0E1EBA9EA3693, 64, 0xFFFFFFFFFFFFFFFF, false, false, 0xFFFFFFFFFFFFFFFF);
            public static Parameters CRC64Xz => new Parameters(0x42F0E1EBA9EA3693, 64, 0xFFFFFFFFFFFFFFFF, true, true, 0xFFFFFFFFFFFFFFFF);
            #endregion
        }

        private readonly Parameters parameters;
        private readonly ulong mask;
        private readonly ulong[] table = new ulong[256];

        public CRC(Parameters parameters)
        {
            this.parameters = parameters;
            mask = ulong.MaxValue >> (64 - parameters.HashSize);
            for (var i = 0; i < table.Length; i++)
                table[i] = CreateTableEntry(i);
        }

        public byte[] ComputeHash(byte[] data)
        {
            var crc = parameters.Init;
            if (parameters.RefOut)
            {
                foreach (var value in data)
                {
                    crc = table[(crc ^ value) & 0xFF] ^ (crc >> 8);
                    crc &= mask;
                }
            }
            else
            {
                var toRight = parameters.HashSize - 8;
                toRight = toRight < 0 ? 0 : toRight;
                foreach (var value in data)
                {
                    crc = table[((crc >> toRight) ^ value) & 0xFF] ^ (crc << 8);
                    crc &= mask;
                }
            }
            var bytes = BitConverter.GetBytes(crc ^ parameters.XorOut);
            if (!BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();
            return bytes.Take((parameters.HashSize + 7) / 8).ToArray();
        }

        private ulong CreateTableEntry(int index)
        {
            var r = (ulong)index;

            if (parameters.RefIn)
                r = ReverseBits(r, parameters.HashSize);
            else if (parameters.HashSize > 8)
                r <<= parameters.HashSize - 8;

            var lastBit = 1ul << (parameters.HashSize - 1);

            for (var i = 0; i < 8; i++)
                if ((r & lastBit) != 0)
                    r = (r << 1) ^ parameters.Poly;
                else
                    r <<= 1;

            if (parameters.RefIn)
                r = ReverseBits(r, (byte)parameters.HashSize);

            return r & mask;
        }

        private static ulong ReverseBits(ulong ul, int valueLength)
        {
            ulong newValue = 0;
            for (var i = valueLength - 1; i >= 0; i--)
            {
                newValue |= (ul & 1) << i;
                ul >>= 1;
            }
            return newValue;
        }
    }
}