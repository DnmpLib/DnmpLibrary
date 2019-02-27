using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Security.Cryptography.Symmetric
{
    public interface ISymmetricKey
    {
        ISymmetricAlgorithm GetAlgorithmInstance();

        byte[] GetBytes();

        ISymmetricKey CreateFromBytes(byte[] data);

        ISymmetricKey GenerateNewKey();
    }
}
