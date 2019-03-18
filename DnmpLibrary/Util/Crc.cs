using System;
using System.Linq;

namespace DnmpLibrary.Util
{
    internal class Crc
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
            public static Parameters Crc3Rohc => new Parameters(0x3, 3, 0x7, true, true, 0x0);
            public static Parameters Crc4Itu => new Parameters(0x3, 4, 0, true, true, 0x0);
            public static Parameters Crc5Epc => new Parameters(0x9, 5, 0x9, false, false, 0x0);
            public static Parameters Crc5Itu => new Parameters(0x15, 5, 0x0, true, true, 0x0);
            public static Parameters Crc5Usb => new Parameters(0x5, 5, 0x1F, true, true, 0x1F);
            public static Parameters Crc6Cdma2000A => new Parameters(0x27, 6, 0x3F, false, false, 0x0);
            public static Parameters Crc6Cdma2000B => new Parameters(0x7, 6, 0x3F, false, false, 0x0);
            public static Parameters Crc6Darc => new Parameters(0x19, 6, 0x0, true, true, 0x0);
            public static Parameters Crc6Itu => new Parameters(0x3, 6, 0x0, true, true, 0x0);
            public static Parameters Crc7 => new Parameters(0x9, 7, 0x0, false, false, 0x0);
            public static Parameters Crc7Rohc => new Parameters(0x4F, 7, 0x7F, true, true, 0x0);
            public static Parameters Crc8 => new Parameters(0x7, 8, 0x0, false, false, 0x0);
            public static Parameters Crc8Cdma2000 => new Parameters(0x9B, 8, 0xFF, false, false, 0x0);
            public static Parameters Crc8Darc => new Parameters(0x39, 8, 0x0, true, true, 0x0);
            public static Parameters Crc8DvbS2 => new Parameters(0xD5, 8, 0x0, false, false, 0x0);
            public static Parameters Crc8Ebu => new Parameters(0x1D, 8, 0xFF, true, true, 0x0);
            public static Parameters Crc8ICode => new Parameters(0x1D, 8, 0xFD, false, false, 0x0);
            public static Parameters Crc8Itu => new Parameters(0x7, 8, 0x0, false, false, 0x55);
            public static Parameters Crc8Maxim => new Parameters(0x31, 8, 0x0, true, true, 0x0);
            public static Parameters Crc8Rohc => new Parameters(0x7, 8, 0xFF, true, true, 0x00);
            public static Parameters Crc8Wcdma => new Parameters(0x9B, 8, 0x0, true, true, 0x0);
            public static Parameters Crc10 => new Parameters(0x233, 10, 0x0, false, false, 0x0);
            public static Parameters Crc10Cdma2000 => new Parameters(0x3D9, 10, 0x3FF, false, false, 0x0);
            public static Parameters Crc11 => new Parameters(0x385, 11, 0x1A, false, false, 0x0);
            public static Parameters Crc123Gpp => new Parameters(0x80F, 12, 0x0, false, true, 0x0);
            public static Parameters Crc12Cdma2000 => new Parameters(0x3D9, 12, 0xFFF, false, false, 0x0);
            public static Parameters Crc12Dect => new Parameters(0x80F, 12, 0x0, false, false, 0x0);
            public static Parameters Crc13Bbc => new Parameters(0x1CF5, 13, 0x0, false, false, 0x0);
            public static Parameters Crc14Darc => new Parameters(0x805, 13, 0x0, true, true, 0x0);
            public static Parameters Crc15 => new Parameters(0x4599, 15, 0x0, false, false, 0x0);
            public static Parameters Crc15Mpt1327 => new Parameters(0x6815, 15, 0x0, false, false, 0x1);
            public static Parameters Crc16Arc => new Parameters(0x8005, 16, 0x0, true, true, 0x0);
            public static Parameters Crc16AugCcitt => new Parameters(0x1021, 16, 0x1D0F, false, false, 0x0);
            public static Parameters Crc16Buypass => new Parameters(0x8005, 16, 0x0, false, false, 0x0);
            public static Parameters Crc16CcittFalse => new Parameters(0x1021, 16, 0xFFFF, false, false, 0x0);
            public static Parameters Crc16Cdma2000 => new Parameters(0xC867, 16, 0xFFFF, false, false, 0x0);
            public static Parameters Crc16Dds110 => new Parameters(0x8006, 16, 0x800D, false, false, 0x0);
            public static Parameters Crc16DectR => new Parameters(0x589, 16, 0x0, false, false, 0x1);
            public static Parameters Crc16DectX => new Parameters(0x589, 16, 0x0, false, false, 0x0);
            public static Parameters Crc16Dnp => new Parameters(0x3D65, 16, 0x0, true, true, 0xFFFF);
            public static Parameters Crc16En13757 => new Parameters(0x3D65, 16, 0x0, false, false, 0xFFFF);
            public static Parameters Crc16Genibus => new Parameters(0x1021, 16, 0xFFFF, false, false, 0xFFFF);
            public static Parameters Crc16Maxim => new Parameters(0x8005, 16, 0x0, true, true, 0xFFFF);
            public static Parameters Crc16Mcrf4XX => new Parameters(0x1021, 16, 0xFFFF, true, true, 0x0);
            public static Parameters Crc16Riello => new Parameters(0x1021, 16, 0xB2AA, true, true, 0x0);
            public static Parameters Crc16T10Diff => new Parameters(0x8BB7, 16, 0x0, false, false, 0x0);
            public static Parameters Crc16Teledisk => new Parameters(0xA097, 16, 0x0, false, false, 0x0);
            public static Parameters Crc16Tms37157 => new Parameters(0x1021, 16, 0x89EC, true, true, 0x0);
            public static Parameters Crc16Usb => new Parameters(0x8005, 16, 0xFFFF, true, true, 0xFFFF);
            public static Parameters CrcA => new Parameters(0x1021, 16, 0xC6C6, true, true, 0x0);
            public static Parameters Crc16Kermit => new Parameters(0x1021, 16, 0x0, true, true, 0x0);
            public static Parameters Crc16Modbus => new Parameters(0x8005, 16, 0xFFFF, true, true, 0x0);
            public static Parameters Crc16X25 => new Parameters(0x1021, 16, 0xFFFF, true, true, 0xFFFF);
            public static Parameters Crc16Xmodem => new Parameters(0x1021, 16, 0x0, false, false, 0x0);
            public static Parameters Crc24 => new Parameters(0x864CFB, 24, 0xB704CE, false, false, 0x0);
            public static Parameters Crc24FlexrayA => new Parameters(0x5D6DCB, 24, 0xFEDCBA, false, false, 0x0);
            public static Parameters Crc24FlexrayB => new Parameters(0x5D6DCB, 24, 0xABCDEF, false, false, 0x0);
            public static Parameters Crc32Philips => new Parameters(0x4C11DB7, 31, 0x7FFFFFFF, false, false, 0x7FFFFFFF);
            public static Parameters Crc32 => new Parameters(0x04C11DB7, 32, 0xFFFFFFFF, true, true, 0xFFFFFFFF);
            public static Parameters Crc32Bzip2 => new Parameters(0x04C11DB7, 32, 0xFFFFFFFF, false, false, 0xFFFFFFFF);
            public static Parameters Crc32C => new Parameters(0x1EDC6F41, 32, 0xFFFFFFFF, true, true, 0xFFFFFFFF);
            public static Parameters Crc32D => new Parameters(0xA833982B, 32, 0xFFFFFFFF, true, true, 0xFFFFFFFF);
            public static Parameters Crc32Mpeg2 => new Parameters(0x04C11DB7, 32, 0xFFFFFFFF, false, false, 0x0);
            public static Parameters Crc32Posix => new Parameters(0x04C11DB7, 32, 0x0, false, false, 0xFFFFFFFF);
            public static Parameters Crc32Q => new Parameters(0x814141AB, 32, 0x0, false, false, 0x0);
            public static Parameters Crc32Jamcrc => new Parameters(0x04C11DB7, 32, 0xFFFFFFFF, true, true, 0x0);
            public static Parameters Crc32Xfer => new Parameters(0x000000AF, 32, 0x0, false, false, 0x0);
            public static Parameters Crc32Zlib => new Parameters(0x4C11DB7, 32, 0xFFFFFFFF, true, true, 0xFFFFFFFF);
            public static Parameters Crc40Gsm => new Parameters(0x4820009, 40, 0x0, false, false, 0xFFFFFFFFFF);
            public static Parameters Crc64 => new Parameters(0x42F0E1EBA9EA3693, 64, 0x0, false, false, 0x0);
            public static Parameters Crc64We => new Parameters(0x42F0E1EBA9EA3693, 64, 0xFFFFFFFFFFFFFFFF, false, false, 0xFFFFFFFFFFFFFFFF);
            public static Parameters Crc64Xz => new Parameters(0x42F0E1EBA9EA3693, 64, 0xFFFFFFFFFFFFFFFF, true, true, 0xFFFFFFFFFFFFFFFF);
            #endregion
        }

        private readonly Parameters parameters;
        private readonly ulong mask;
        private readonly ulong[] table = new ulong[256];

        public Crc(Parameters parameters)
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