using System.Collections.Generic;
using System.Net;

namespace DNMPLibrary.Interaction.Protocol.EndPointImpl
{

    public class RealIPEndPoint : IEndPoint
    {
        public readonly IPEndPoint RealEndPoint;

        public RealIPEndPoint(IPEndPoint realEndPoint)
        {
            RealEndPoint = realEndPoint;
        }

        public override bool Equals(object obj)
        {
            return obj is RealIPEndPoint point &&
                   EqualityComparer<IPEndPoint>.Default.Equals(RealEndPoint, point.RealEndPoint);
        }

        public override int GetHashCode()
        {
            return -1407700588 + EqualityComparer<IPEndPoint>.Default.GetHashCode(RealEndPoint);
        }

        public override string ToString()
        {
            return RealEndPoint.ToString();
        }
    }
}
