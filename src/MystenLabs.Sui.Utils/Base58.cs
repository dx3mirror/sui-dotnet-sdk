namespace MystenLabs.Sui.Utils;

/// <summary>
/// Encodes and decodes byte sequences using Base58 (Bitcoin alphabet), compatible with @scure/base.
/// </summary>
public static class Base58
{
    /// <summary>
    /// Encodes bytes as a Base58 string using the Bitcoin alphabet.
    /// </summary>
    /// <param name="bytes">Bytes to encode.</param>
    /// <returns>Base58-encoded string.</returns>
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        return SimpleBase.Base58.Bitcoin.Encode(bytes.ToArray());
    }

    /// <summary>
    /// Decodes a Base58 string into a byte array using the Bitcoin alphabet.
    /// </summary>
    /// <param name="value">Base58-encoded string.</param>
    /// <returns>Decoded bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when the string contains invalid Base58 characters.</exception>
    public static byte[] Decode(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return [];
        }

        return SimpleBase.Base58.Bitcoin.Decode(value.ToString());
    }
}
