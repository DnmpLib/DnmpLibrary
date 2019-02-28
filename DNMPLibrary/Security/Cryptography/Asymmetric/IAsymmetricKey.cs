namespace DNMPLibrary.Security.Cryptography.Asymmetric
{
    public interface IAsymmetricKey
    {
        IAsymmetricAlgorithm GetAlgorithmInstance();

        byte[] GetNetworkId();
    }
}
