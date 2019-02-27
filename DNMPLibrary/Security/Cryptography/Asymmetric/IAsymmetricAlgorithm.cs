using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Security.Cryptography.Asymmetric
{
    public interface IAsymmetricAlgorithm
    {
        byte[] Encrypt(IAsymmetricKey key, byte[] data);
        byte[] Decrypt(IAsymmetricKey key, byte[] data);
        byte[] Sign(IAsymmetricKey key, byte[] data);
        bool Verify(IAsymmetricKey key, byte[] data, byte[] signature);
    }
}
