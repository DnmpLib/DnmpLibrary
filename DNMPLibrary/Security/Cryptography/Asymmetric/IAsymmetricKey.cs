using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Security.Cryptography.Asymmetric
{
    public interface IAsymmetricKey
    {
        IAsymmetricAlgorithm GetAlgorithmInstance();

        byte[] GetNetworkId();
    }
}
