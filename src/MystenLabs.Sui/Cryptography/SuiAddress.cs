namespace MystenLabs.Sui.Cryptography;

using MystenLabs.Sui.Utils;

/// <summary>
/// Sui address normalization (0x + 64 hex chars = 32 bytes).
/// </summary>
public static class SuiAddress
{
    /// <summary>
    /// Length of a Sui address in bytes (before hex encoding).
    /// </summary>
    public const int AddressLengthBytes = 32;

    /// <summary>
    /// Length of a normalized Sui address string (0x + 64 hex chars).
    /// </summary>
    public const int NormalizedAddressLength = 2 + (AddressLengthBytes * 2);

    private const string AddressPrefix = "0x";

    /// <summary>
    /// Returns true if the value is a valid Sui address: optional 0x prefix and exactly 32 bytes of hex.
    /// </summary>
    /// <param name="value">Address string to check (can include 0x prefix).</param>
    /// <returns>True if valid (hex and byte length equals <see cref="AddressLengthBytes"/>).</returns>
    public static bool IsValidSuiAddress(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return false;
        }

        ReadOnlySpan<char> hex = value.Trim();
        if (hex.StartsWith(AddressPrefix))
        {
            hex = hex[AddressPrefix.Length..];
        }

        if (hex.Length != AddressLengthBytes * 2)
        {
            return false;
        }

        for (int index = 0; index < hex.Length; index++)
        {
            char character = hex[index];
            if (!Hex.IsHexChar(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true if the value is a valid Sui object ID (same format as address: 0x + 64 hex).
    /// </summary>
    public static bool IsValidSuiObjectId(ReadOnlySpan<char> value)
    {
        return IsValidSuiAddress(value);
    }

    /// <summary>
    /// Normalizes an address to 0x followed by 64 lowercase hex characters (32 bytes).
    /// Shorter addresses are left-padded with zeros; longer are truncated.
    /// </summary>
    public static string Normalize(ReadOnlySpan<char> address)
    {
        ReadOnlySpan<char> hex = address;
        if (hex.StartsWith(AddressPrefix))
        {
            hex = hex[AddressPrefix.Length..];
        }

        if (hex.Length >= AddressLengthBytes * 2)
        {
            return AddressPrefix + hex[..(AddressLengthBytes * 2)].ToString().ToLowerInvariant();
        }

        int padLength = (AddressLengthBytes * 2) - hex.Length;
        return AddressPrefix + new string('0', padLength) + hex.ToString().ToLowerInvariant();
    }
}
