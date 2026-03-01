namespace MystenLabs.Sui.Cryptography;

using System;
using MystenLabs.Sui.Utils;

/// <summary>
/// Sui transaction digest validation (32 bytes, Base58-encoded).
/// </summary>
public static class TransactionDigest
{
    /// <summary>
    /// Length of a transaction digest in bytes (serialization format).
    /// </summary>
    public const int DigestLengthBytes = 32;

    /// <summary>
    /// Returns true if the value is a valid Sui transaction digest (Base58 decoding yields exactly 32 bytes).
    /// </summary>
    /// <param name="value">Base58-encoded digest string.</param>
    /// <returns>True if valid.</returns>
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return false;
        }

        try
        {
            byte[] decoded = Base58.Decode(value.Trim());
            return decoded.Length == DigestLengthBytes;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
