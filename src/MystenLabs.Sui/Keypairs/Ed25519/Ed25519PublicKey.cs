namespace MystenLabs.Sui.Keypairs.Ed25519;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Utils;

/// <summary>
/// Ed25519 public key (32 bytes) for Sui.
/// </summary>
public sealed class Ed25519PublicKey : PublicKey
{
    /// <summary>
    /// Size of an Ed25519 public key in bytes.
    /// </summary>
    public const int Size = SignatureSchemeConstants.Ed25519PublicKeySize;

    private readonly byte[] _key;

    /// <summary>
    /// Creates an Ed25519 public key from raw 32-byte key or base64 string.
    /// </summary>
    /// <param name="value">32 bytes or base64-encoded public key.</param>
    public Ed25519PublicKey(byte[] value)
    {
        if (value == null || value.Length != Size)
        {
            throw new ArgumentException($"Ed25519 public key must be {Size} bytes.", nameof(value));
        }

        _key = (byte[])value.Clone();
    }

    /// <summary>
    /// Creates an Ed25519 public key from a base64-encoded string.
    /// </summary>
    public static Ed25519PublicKey FromBase64(string base64)
    {
        byte[] bytes = Base64.Decode(base64.AsSpan());
        return new Ed25519PublicKey(bytes);
    }

    /// <inheritdoc />
    public override byte[] ToRawBytes()
    {
        return (byte[])_key.Clone();
    }

    /// <inheritdoc />
    public override byte Flag()
    {
        return (byte)SignatureScheme.Ed25519;
    }

    /// <inheritdoc />
    public override bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        if (signature.Length == SignatureSchemeConstants.Ed25519SignatureSize)
        {
            return VerifyRaw(data, signature);
        }

        if (signature.Length > 0)
        {
            try
            {
                string base64String = System.Text.Encoding.UTF8.GetString(signature.ToArray());
                (SignatureScheme scheme, byte[] signatureBytes, byte[] publicKeyBytes) = Signature.ParseSerializedKeypairSignature(base64String);
                if (scheme != SignatureScheme.Ed25519)
                {
                    return false;
                }

                if (!Equals(new Ed25519PublicKey(publicKeyBytes)))
                {
                    return false;
                }

                return VerifyRaw(data, signatureBytes);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifies a base64 serialized signature (flag + signature + public key).
    /// </summary>
    public bool Verify(ReadOnlySpan<byte> data, string serializedSignature)
    {
        (SignatureScheme scheme, byte[] signatureBytes, byte[] publicKeyBytes) = Signature.ParseSerializedKeypairSignature(serializedSignature);
        if (scheme != SignatureScheme.Ed25519)
        {
            return false;
        }

        if (!Equals(new Ed25519PublicKey(publicKeyBytes)))
        {
            return false;
        }

        return VerifyRaw(data, signatureBytes);
    }

    private bool VerifyRaw(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        if (signature.Length != SignatureSchemeConstants.Ed25519SignatureSize)
        {
            return false;
        }

        return Ed25519Impl.Verify(signature, data, _key);
    }
}
