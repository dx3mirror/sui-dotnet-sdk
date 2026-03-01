namespace MystenLabs.Sui.Transactions;

using MystenLabs.Sui.Cryptography;

/// <summary>
/// Hashes typed transaction data for signing (domain-separated Blake2b).
/// </summary>
public static class TransactionHasher
{
    private const string TransactionDataTypeTag = "TransactionData";

    /// <summary>
    /// Computes the 32-byte Blake2b hash of the type tag prefix plus data (for signing).
    /// Format: "{typeTag}::" as UTF-8 bytes, then data.
    /// </summary>
    /// <param name="typeTag">Type tag (e.g. TransactionData, SenderSignedData).</param>
    /// <param name="data">Data to hash (e.g. serialized transaction).</param>
    /// <returns>32-byte hash.</returns>
    public static byte[] HashTypedData(string typeTag, ReadOnlySpan<byte> data)
    {
        string prefix = typeTag + "::";
        int prefixLength = System.Text.Encoding.UTF8.GetByteCount(prefix);
        byte[] combined = new byte[prefixLength + data.Length];
        System.Text.Encoding.UTF8.GetBytes(prefix, 0, prefix.Length, combined, 0);
        data.CopyTo(combined.AsSpan(prefixLength));
        return Blake2b.Hash256(combined);
    }

    /// <summary>
    /// Computes the digest to sign for transaction data (TransactionData:: + serialized tx bytes).
    /// </summary>
    /// <param name="serializedTransaction">BCS-serialized transaction data (e.g. V1 or V2).</param>
    /// <returns>32-byte digest to pass to the signer.</returns>
    public static byte[] GetDigestToSign(ReadOnlySpan<byte> serializedTransaction)
    {
        return HashTypedData(TransactionDataTypeTag, serializedTransaction);
    }
}
