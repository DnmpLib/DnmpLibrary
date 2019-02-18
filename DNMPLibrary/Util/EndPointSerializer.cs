using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Util
{
    public static class EndPointSerializer //TODO make this unified
    {
        public static byte[] ToBytes(EndPoint endPoint)
        {
            var buf = new byte[6];
            for (var i = 0; i < 4; i++)
                buf[i] = ((IPEndPoint)endPoint).Address.GetAddressBytes()[i];
            buf[4] = (byte)(((IPEndPoint)endPoint).Port / 256);
            buf[5] = (byte)(((IPEndPoint)endPoint).Port % 256);
            return buf;
        }

        public static EndPoint FromBytes(byte[] data)
        {
            var tEndPoint = new IPEndPoint(IPAddress.Parse(string.Join(".", data.Take(4))), data[4] * 256 + data[5]);
            return tEndPoint;
        }
    }
}
