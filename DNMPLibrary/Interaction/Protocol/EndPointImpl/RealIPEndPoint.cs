using System.Net;

namespace DNMPLibrary.Interaction.Protocol.EndPointImpl
{

    public class RealIPEndPoint : IEndPoint
    {
        public IPEndPoint RealEndPoint;

        public RealIPEndPoint(IPEndPoint realEndPoint)
        {
            RealEndPoint = realEndPoint;
        }
    }
}
