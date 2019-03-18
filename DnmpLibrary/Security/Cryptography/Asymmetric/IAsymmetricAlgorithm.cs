namespace DnmpLibrary.Security.Cryptography.Asymmetric
{
    public interface IAsymmetricAlgorithm
    {
        byte[] Encrypt(IAsymmetricKey key, byte[] data);
        byte[] Decrypt(IAsymmetricKey key, byte[] data);
        byte[] Sign(IAsymmetricKey key, byte[] data);
        bool Verify(IAsymmetricKey key, byte[] data, byte[] signature);
    }
}
