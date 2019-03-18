namespace DnmpLibrary.Security.Cryptography.Asymmetric
{
    public interface IAsymmetricKey
    {
        IAsymmetricAlgorithm GetAlgorithmInstance();

        byte[] GetNetworkId();
    }
}
