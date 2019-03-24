using System;
using System.IO;
using System.Text;

namespace DnmpLibrary.Util.BigEndian
{
    public class BigEndianBinaryWriter : BinaryWriter
    {
        private byte[] buffer = new byte[32];

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

        public BigEndianBinaryWriter(Stream output) : base(output) { }

        public BigEndianBinaryWriter(Stream output, Encoding encoding) : base(output, encoding) { }

        public BigEndianBinaryWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen) { }

        public override void Write(double value)
        {
            buffer = BitConverter.GetBytes(value);
            ReverseBuffer(8);
            BaseStream.Write(buffer, 0, buffer.Length);
        }

        public override void Write(decimal value)
        {
            var bits = decimal.GetBits(value);
            Write(bits[3]);
            Write(bits[2]);
            Write(bits[1]);
            Write(bits[0]);
        }

        public override void Write(short value)
        {
            buffer = BitConverter.GetBytes(value);
            ReverseBuffer(2);
            BaseStream.Write(buffer, 0, buffer.Length);
        }

        public override void Write(ushort value)
        {
            buffer = BitConverter.GetBytes(value);
            ReverseBuffer(2);
            BaseStream.Write(buffer, 0, buffer.Length);
        }

        public override void Write(int value)
        {
            buffer = BitConverter.GetBytes(value);
            ReverseBuffer(4);
            BaseStream.Write(buffer, 0, buffer.Length);
        }

        public override void Write(uint value)
        {
            buffer = BitConverter.GetBytes(value);
            ReverseBuffer(4);
            BaseStream.Write(buffer, 0, buffer.Length);
        }

        public override void Write(long value)
        {
            buffer = BitConverter.GetBytes(value);
            ReverseBuffer(8);
            BaseStream.Write(buffer, 0, buffer.Length);
        }

        public override void Write(ulong value)
        {
            buffer = BitConverter.GetBytes(value);
            ReverseBuffer(8);
            BaseStream.Write(buffer, 0, buffer.Length);
        }

        public override void Write(float value)
        {
            buffer = BitConverter.GetBytes(value);
            ReverseBuffer(4);
            BaseStream.Write(buffer, 0, buffer.Length);
        }
    }
}
