using System.Linq;
using System.Net;
using DNMPLibrary.Interaction.Protocol.EndPointImpl;

namespace DNMPLibrary.Interaction.Protocol.EndPointFactoryImpl
{
    public class RealIPEndPointFactory : IEndPointFactory
    {
        public byte[] SerializeEndPoint(IEndPoint endPoint)
        {
            if (!(endPoint is RealIPEndPoint))
                return null;
            var realIpEndPoint = (RealIPEndPoint) endPoint;
            var networkEndPoint = realIpEndPoint.RealEndPoint;
            var buf = new byte[6];
            for (var i = 0; i < 4; i++)
                buf[i] = networkEndPoint.Address.GetAddressBytes()[i];
            buf[4] = (byte)(networkEndPoint.Port / 256);
            buf[5] = (byte)(networkEndPoint.Port % 256);
            return buf;
        }

        public IEndPoint DeserializeEndPoint(byte[] data)
        {
            return new RealIPEndPoint(new IPEndPoint(IPAddress.Parse(string.Join(".", data.Take(4))),
                data[4] * 256 + data[5]));
        }
    }
}
