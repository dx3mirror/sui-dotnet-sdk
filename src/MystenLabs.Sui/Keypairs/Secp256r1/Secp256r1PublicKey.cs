namespace MystenLabs.Sui.Keypairs.Secp256r1;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Utils;

/// <summary>
/// Secp256r1 (P-256) compressed public key (33 bytes) for Sui.
/// </summary>
public sealed class Secp256r1PublicKey : PublicKey
{
    /// <summary>
    /// Size of a compressed Secp256r1 public key in bytes.
    /// </summary>
    public const int Size = SignatureSchemeConstants.Secp256r1PublicKeySize;

    private readonly byte[] _key;

    /// <summary>
    /// Creates a Secp256r1 public key from raw 33-byte compressed key or base64 string.
    /// </summary>
    /// <param name="value">33 bytes or base64-encoded public key.</param>
    public Secp256r1PublicKey(byte[] value)
    {
        if (value == null || value.Length != Size)
        {
            throw new ArgumentException($"Secp256r1 public key must be {Size} bytes.", nameof(value));
        }

        _key = (byte[])value.Clone();
    }

    /// <summary>
    /// Creates a Secp256r1 public key from a base64-encoded string.
    /// </summary>
    public static Secp256r1PublicKey FromBase64(string base64)
    {
        byte[] bytes = Base64.Decode(base64.AsSpan());
        return new Secp256r1PublicKey(bytes);
    }

    /// <inheritdoc />
    public override byte[] ToRawBytes()
    {
        return (byte[])_key.Clone();
    }

    /// <inheritdoc />
    public override byte Flag()
    {
        return (byte)SignatureScheme.Secp256r1;
    }

    /// <inheritdoc />
    public override bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        if (signature.Length == SignatureSchemeConstants.Secp256r1SignatureSize)
        {
            return Secp256r1Impl.Verify(signature, data, _key);
        }

        if (signature.Length > 0)
        {
            try
            {
                string base64String = System.Text.Encoding.UTF8.GetString(signature.ToArray());
                (SignatureScheme scheme, byte[] signatureBytes, byte[] publicKeyBytes) =
                    Signature.ParseSerializedKeypairSignature(base64String);
                if (scheme != SignatureScheme.Secp256r1)
                {
                    return false;
                }

                if (!_key.AsSpan().SequenceEqual(publicKeyBytes))
                {
                    return false;
                }

                return Secp256r1Impl.Verify(signatureBytes, data, _key);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
