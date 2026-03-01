namespace MystenLabs.Sui.Utils;

/// <summary>
/// Encodes and decodes byte sequences using standard Base64.
/// </summary>
public static class Base64
{
    /// <summary>
    /// Chunk size used when encoding large byte sequences (matches TS reference for compatibility).
    /// </summary>
    public const int LargeDataChunkSize = 8192;

    /// <summary>
    /// Encodes bytes as a Base64 string.
    /// </summary>
    /// <param name="bytes">Bytes to encode.</param>
    /// <returns>Base64-encoded string.</returns>
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decodes a Base64 string into a byte array.
    /// </summary>
    /// <param name="value">Base64-encoded string.</param>
    /// <returns>Decoded bytes.</returns>
    /// <exception cref="FormatException">Thrown when the string is not valid Base64.</exception>
    public static byte[] Decode(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return [];
        }

        return Convert.FromBase64String(value.ToString());
    }
}
