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
