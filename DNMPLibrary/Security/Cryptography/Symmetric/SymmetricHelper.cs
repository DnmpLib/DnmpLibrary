namespace DNMPLibrary.Security.Cryptography.Symmetric
{
    public static class SymmetricHelper
    {
        public static byte[] Encrypt(ISymmetricKey key, byte[] data)
        {
            return key.GetAlgorithmInstance().Encrypt(key, data);
        }

        public static byte[] Decrypt(ISymmetricKey key, byte[] data)
        {
            return key.GetAlgorithmInstance().Decrypt(key, data);
        }
    }
}
