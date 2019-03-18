using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnmpLibrary.Util.BigEndian
{
    public class BigEndianBinaryReader : BinaryReader
    {
        private readonly byte[] buffer = new byte[32];

        private byte tByte;

        private void ReverseBuffer(int count)
        {
            for (var i = 0; i < count / 2; i++)
            {
                tByte = buffer[i];
                buffer[i] = buffer[count - i - 1];
                buffer[count - i - 1] = tByte;
            }
        }

        public BigEndianBinaryReader(Stream input) : base(input) { }
        public BigEndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding) { }
        public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

        public override decimal ReadDecimal()
        {
            FillBuffer(16);
            ReverseBuffer(16);

            var i1 = BitConverter.ToInt32(buffer, 0);
            var i2 = BitConverter.ToInt32(buffer, 4);
            var i3 = BitConverter.ToInt32(buffer, 8);
            var i4 = BitConverter.ToInt32(buffer, 12);

            return new decimal(new [] { i1, i2, i3, i4 });
        }

        public override double ReadDouble()
        {
            FillBuffer(8);
            ReverseBuffer(8);
            return BitConverter.ToDouble(buffer, 0);
        }

        public override short ReadInt16()
        {
            FillBuffer(2);
            ReverseBuffer(2);
            return BitConverter.ToInt16(buffer, 0);
        }

        public override int ReadInt32()
        {
            FillBuffer(4);
            ReverseBuffer(4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public override long ReadInt64()
        {
            FillBuffer(8);
            ReverseBuffer(8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public override float ReadSingle()
        {
            FillBuffer(4);
            ReverseBuffer(4);
            return BitConverter.ToSingle(buffer, 0);
        }

        public override ushort ReadUInt16()
        {
            FillBuffer(2);
            ReverseBuffer(2);
            return BitConverter.ToUInt16(buffer, 0);
        }

        public override uint ReadUInt32()
        {
            FillBuffer(4);
            ReverseBuffer(4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public override ulong ReadUInt64()
        {
            FillBuffer(8);
            ReverseBuffer(8);
            return BitConverter.ToUInt64(buffer, 0);
        }

        protected override void FillBuffer(int numBytes)
        {
            if (numBytes < 0 || numBytes > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(numBytes));
            var offset = 0;
            if (numBytes == 1)
            {
                var num = BaseStream.ReadByte();
                if (num == -1)
                    throw new EndOfStreamException();
                buffer[0] = (byte)num;
            }
            else
            {
                do
                {
                    var num = BaseStream.Read(buffer, offset, numBytes - offset);
                    if (num == 0)
                        throw new EndOfStreamException();
                    offset += num;
                }
                while (offset < numBytes);
            }
        }
    }
}
