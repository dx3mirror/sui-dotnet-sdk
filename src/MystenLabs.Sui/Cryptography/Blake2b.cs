namespace MystenLabs.Sui.Cryptography;

using System.Buffers;
using Org.BouncyCastle.Crypto.Digests;

/// <summary>
/// Blake2b hash (256-bit / 32-byte output) for Sui address derivation and intent hashing.
/// </summary>
public static class Blake2b
{
    private const int DefaultDigestLength = 32;
    private const int BitsPerByte = 8;

    /// <summary>
    /// Computes the 32-byte Blake2b hash of the input.
    /// </summary>
    /// <param name="data">Input data.</param>
    /// <returns>32-byte hash.</returns>
    public static byte[] Hash256(ReadOnlySpan<byte> data)
    {
        var digest = new Blake2bDigest(DefaultDigestLength * BitsPerByte);
        byte[]? buffer = null;
        try
        {
            buffer = ArrayPool<byte>.Shared.Rent(data.Length);
            data.CopyTo(buffer);
            digest.BlockUpdate(buffer, 0, data.Length);
        }
        finally
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        byte[] result = new byte[DefaultDigestLength];
        digest.DoFinal(result, 0);
        return result;
    }
}
