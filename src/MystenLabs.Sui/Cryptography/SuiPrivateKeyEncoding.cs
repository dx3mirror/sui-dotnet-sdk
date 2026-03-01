namespace MystenLabs.Sui.Cryptography;

/// <summary>
/// Sui private key Bech32 prefix (33 bytes: flag + 32-byte secret).
/// </summary>
public static class SuiPrivateKeyEncoding
{
    /// <summary>
    /// Bech32 human-readable part for Sui private keys.
    /// </summary>
    public const string SuiPrivateKeyPrefix = "suiprivkey";

    private const int SchemeFlagLengthBytes = 1;
    private const int SchemeFlagIndex = 0;
    private const int SecretKeyOffsetInEncodedBytes = 1;

    /// <summary>
    /// Decodes a Bech32-encoded Sui private key (suiprivkey1...) to scheme and 32-byte secret key.
    /// </summary>
    /// <param name="encoded">Bech32 string (e.g. suiprivkey1...).</param>
    /// <returns>Signature scheme and 32-byte secret key.</returns>
    public static (SignatureScheme Scheme, byte[] SecretKey) Decode(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
        {
            throw new ArgumentNullException(nameof(encoded));
        }

        (string prefix, byte[] data) = Bech32.Decode(encoded);
        if (prefix != SuiPrivateKeyPrefix)
        {
            throw new ArgumentException($"Invalid private key prefix; expected {SuiPrivateKeyPrefix}.", nameof(encoded));
        }

        if (data.Length != KeypairConstants.PrivateKeySize + SchemeFlagLengthBytes)
        {
            throw new ArgumentException(
                $"Invalid private key length: expected {KeypairConstants.PrivateKeySize + SchemeFlagLengthBytes} bytes, got {data.Length}.",
                nameof(encoded));
        }

        byte flag = data[SchemeFlagIndex];
        if (!Enum.IsDefined(typeof(SignatureScheme), (int)flag))
        {
            throw new ArgumentException($"Unsupported scheme flag: {flag}.", nameof(encoded));
        }

        var scheme = (SignatureScheme)flag;
        byte[] secretKey = new byte[KeypairConstants.PrivateKeySize];
        Buffer.BlockCopy(data, SecretKeyOffsetInEncodedBytes, secretKey, 0, KeypairConstants.PrivateKeySize);
        return (scheme, secretKey);
    }

    /// <summary>
    /// Encodes a 32-byte secret key and scheme to Bech32 (suiprivkey1...).
    /// </summary>
    public static string Encode(byte[] secretKey, SignatureScheme scheme)
    {
        if (secretKey == null || secretKey.Length != KeypairConstants.PrivateKeySize)
        {
            throw new ArgumentException(
                $"Secret key must be {KeypairConstants.PrivateKeySize} bytes.",
                nameof(secretKey));
        }

        byte[] data = new byte[KeypairConstants.PrivateKeySize + SchemeFlagLengthBytes];
        data[SchemeFlagIndex] = (byte)scheme;
        Buffer.BlockCopy(secretKey, 0, data, SecretKeyOffsetInEncodedBytes, KeypairConstants.PrivateKeySize);
        return Bech32.Encode(SuiPrivateKeyPrefix, data);
    }
}
