namespace MystenLabs.Sui.Bcs;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Utils;

/// <summary>
/// Sui address length in bytes (32 bytes = 64 hex chars with 0x prefix).
/// </summary>
public static class SuiBcsConstants
{
    public const int AddressLength = 32;
}

/// <summary>
/// Sui BCS type registrations for Address, ObjectId, and related types used in transactions and RPC.
/// </summary>
public static class SuiBcsTypes
{
    /// <summary>
    /// BCS type for Sui Address (32 bytes). Serializes from/to normalized hex string (0x + 64 hex chars).
    /// </summary>
    public static BcsType<string> Address { get; } = new BcsType<string>(
        "Address",
        reader =>
        {
            byte[] bytes = reader.ReadBytes(SuiBcsConstants.AddressLength);
            return SuiAddress.Normalize(Hex.Encode(bytes));
        },
        (value, writer) =>
        {
            string normalized = SuiAddress.Normalize(value.AsSpan());
            byte[] bytes = Hex.Decode(normalized.AsSpan().StartsWith("0x") ? normalized.AsSpan()[2..] : normalized.AsSpan());
            if (bytes.Length != SuiBcsConstants.AddressLength)
            {
                throw new ArgumentException($"Address must be {SuiBcsConstants.AddressLength} bytes after normalization.", nameof(value));
            }

            writer.WriteBytes(bytes);
        },
        _ => SuiBcsConstants.AddressLength,
        value =>
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Address cannot be null or empty.", nameof(value));
            }

            string normalized = SuiAddress.Normalize(value.AsSpan());
            byte[] bytes = Hex.Decode(normalized.StartsWith("0x") ? normalized.AsSpan()[2..] : normalized.AsSpan());
            if (bytes.Length != SuiBcsConstants.AddressLength)
            {
                throw new ArgumentException($"Invalid address length: expected {SuiBcsConstants.AddressLength} bytes.", nameof(value));
            }
        });

    /// <summary>
    /// BCS type for ObjectId (same as Address, 32 bytes). Use for object references.
    /// </summary>
    public static BcsType<string> ObjectId { get; } = Address;
}
