namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Supported string encodings for BCS byte representation (e.g. in ToString / FromHex).
/// </summary>
public enum BcsEncoding
{
    /// <summary>
    /// Hexadecimal (lowercase, no prefix).
    /// </summary>
    Hex,

    /// <summary>
    /// Base64.
    /// </summary>
    Base64,

    /// <summary>
    /// Base58 (Bitcoin alphabet).
    /// </summary>
    Base58,
}
