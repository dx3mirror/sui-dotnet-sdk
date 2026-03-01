namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Encoding/decoding helpers for BCS bytes to string and lexicographic comparison (e.g. for Map key ordering).
/// </summary>
public static class BcsEncodeDecode
{
    /// <summary>
    /// Encodes bytes to a string using the specified encoding.
    /// </summary>
    /// <param name="data">Bytes to encode.</param>
    /// <param name="encoding">Encoding to use.</param>
    /// <returns>Encoded string.</returns>
    public static string EncodeStr(ReadOnlySpan<byte> data, BcsEncoding encoding)
    {
        return encoding switch
        {
            BcsEncoding.Hex => MystenLabs.Sui.Utils.Hex.Encode(data),
            BcsEncoding.Base64 => MystenLabs.Sui.Utils.Base64.Encode(data),
            BcsEncoding.Base58 => MystenLabs.Sui.Utils.Base58.Encode(data),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "Unsupported encoding.")
        };
    }

    /// <summary>
    /// Decodes a string to bytes using the specified encoding.
    /// </summary>
    /// <param name="data">Encoded string.</param>
    /// <param name="encoding">Encoding that was used.</param>
    /// <returns>Decoded bytes.</returns>
    public static byte[] DecodeStr(ReadOnlySpan<char> data, BcsEncoding encoding)
    {
        return encoding switch
        {
            BcsEncoding.Hex => MystenLabs.Sui.Utils.Hex.Decode(data),
            BcsEncoding.Base64 => MystenLabs.Sui.Utils.Base64.Decode(data),
            BcsEncoding.Base58 => MystenLabs.Sui.Utils.Base58.Decode(data),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "Unsupported encoding.")
        };
    }

    /// <summary>
    /// Compares two byte arrays lexicographically (byte-by-byte, then by length).
    /// Matches Rust Ord for Vec&lt;u8&gt; used for BTreeMap key ordering.
    /// </summary>
    /// <param name="first">First sequence.</param>
    /// <param name="second">Second sequence.</param>
    /// <returns>Negative if first &lt; second, zero if equal, positive if first &gt; second.</returns>
    public static int CompareBcsBytes(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
    {
        int minLength = Math.Min(first.Length, second.Length);
        for (int index = 0; index < minLength; index++)
        {
            int comparison = first[index].CompareTo(second[index]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return first.Length - second.Length;
    }
}
