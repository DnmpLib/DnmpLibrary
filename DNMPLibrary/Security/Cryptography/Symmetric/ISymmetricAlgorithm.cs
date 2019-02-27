namespace DNMPLibrary.Security.Cryptography.Symmetric
{
    public interface ISymmetricAlgorithm
    {
        byte[] Encrypt(ISymmetricKey key, byte[] data);
        byte[] Decrypt(ISymmetricKey key, byte[] data);
    }
}
