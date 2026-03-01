namespace MystenLabs.Sui.Utils;

using System;

/// <summary>
/// Formatting helpers for display (address and digest shortening).
/// </summary>
public static class Format
{
    private const string Ellipsis = "\u2026";

    /// <summary>
    /// Shortens an address for display: 0x + first 4 hex chars + ellipsis + last 4 hex chars.
    /// If length is 6 or less, returns the address as-is.
    /// </summary>
    /// <param name="address">Sui address (with or without 0x).</param>
    /// <returns>Shortened string (e.g. 0x1a2b...c3d4).</returns>
    public static string FormatAddress(ReadOnlySpan<char> address)
    {
        if (address.Length <= 6)
        {
            return address.ToString();
        }

        int offset = address.StartsWith("0x") ? 2 : 0;
        int remaining = address.Length - offset;
        if (remaining <= 8)
        {
            return address.ToString();
        }

        return string.Concat(
            "0x",
            address.Slice(offset, 4).ToString(),
            Ellipsis,
            address.Slice(address.Length - 4, 4).ToString());
    }

    /// <summary>
    /// Shortens a digest for display: first 10 characters + ellipsis.
    /// </summary>
    /// <param name="digest">Transaction or object digest string.</param>
    /// <returns>Shortened string (e.g. "5j7s8K...").</returns>
    public static string FormatDigest(ReadOnlySpan<char> digest)
    {
        if (digest.Length <= 10)
        {
            return digest.ToString();
        }

        return digest.Slice(0, 10).ToString() + Ellipsis;
    }
}
