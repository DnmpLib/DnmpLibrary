using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
