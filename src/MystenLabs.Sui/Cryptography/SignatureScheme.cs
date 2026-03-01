namespace MystenLabs.Sui.Cryptography;

/// <summary>
/// Signature scheme identifier used in Sui serialized keys and signatures.
/// </summary>
public enum SignatureScheme
{
    /// <summary>
    /// Ed25519.
    /// </summary>
    Ed25519 = 0x00,

    /// <summary>
    /// Secp256k1 (secp256k1 curve).
    /// </summary>
    Secp256k1 = 0x01,

    /// <summary>
    /// Secp256r1 (P-256).
    /// </summary>
    Secp256r1 = 0x02,

    /// <summary>
    /// Multi-signature.
    /// </summary>
    MultiSig = 0x03,

    /// <summary>
    /// ZkLogin.
    /// </summary>
    ZkLogin = 0x05,

    /// <summary>
    /// Passkey (WebAuthn).
    /// </summary>
    Passkey = 0x06,
}

/// <summary>
/// Constants for signature scheme flags and public key/signature sizes.
/// </summary>
public static class SignatureSchemeConstants
{
    /// <summary>
    /// Ed25519 public key size in bytes.
    /// </summary>
    public const int Ed25519PublicKeySize = 32;

    /// <summary>
    /// Ed25519 signature size in bytes.
    /// </summary>
    public const int Ed25519SignatureSize = 64;

    /// <summary>
    /// Secp256k1 compressed public key size in bytes.
    /// </summary>
    public const int Secp256k1PublicKeySize = 33;

    /// <summary>
    /// Secp256k1 signature size (compact form) in bytes.
    /// </summary>
    public const int Secp256k1SignatureSize = 64;

    /// <summary>
    /// Secp256r1 compressed public key size in bytes.
    /// </summary>
    public const int Secp256r1PublicKeySize = 33;

    /// <summary>
    /// Secp256r1 signature size in bytes.
    /// </summary>
    public const int Secp256r1SignatureSize = 64;

    /// <summary>
    /// Returns the public key size in bytes for the given scheme (single-signature schemes only).
    /// </summary>
    public static int GetPublicKeySize(SignatureScheme scheme)
    {
        return scheme switch
        {
            SignatureScheme.Ed25519 => Ed25519PublicKeySize,
            SignatureScheme.Secp256k1 => Secp256k1PublicKeySize,
            SignatureScheme.Secp256r1 => Secp256r1PublicKeySize,
            SignatureScheme.Passkey => 33,
            _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, "Unsupported scheme for size.")
        };
    }

    /// <summary>
    /// Returns the signature size in bytes for the given scheme (single-signature schemes only).
    /// </summary>
    public static int GetSignatureSize(SignatureScheme scheme)
    {
        return scheme switch
        {
            SignatureScheme.Ed25519 => Ed25519SignatureSize,
            SignatureScheme.Secp256k1 => Secp256k1SignatureSize,
            SignatureScheme.Secp256r1 => Secp256r1SignatureSize,
            _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, "Unsupported scheme for size.")
        };
    }
}
