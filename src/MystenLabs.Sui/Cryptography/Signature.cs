namespace MystenLabs.Sui.Cryptography;

using MystenLabs.Sui.Utils;

/// <summary>
/// Serializes and parses Sui keypair signatures (flag + signature + public key).
/// For MultiSig use <see cref="MystenLabs.Sui.Multisig.MultiSigSignature.ParseSerialized"/>.
/// </summary>
public static class Signature
{
    private const int SchemeFlagLengthBytes = 1;
    private const int MinSerializedSignatureLengthBytes = 2;

    /// <summary>
    /// Serializes signature + public key to base64: flag (1) + signature + raw public key.
    /// </summary>
    public static string ToSerializedSignature(SignatureScheme scheme, ReadOnlySpan<byte> signature, PublicKey publicKey)
    {
        if (publicKey == null)
        {
            throw new ArgumentNullException(nameof(publicKey));
        }

        byte[] rawPublicKey = publicKey.ToRawBytes();
        int totalLength = SchemeFlagLengthBytes + signature.Length + rawPublicKey.Length;
        byte[] result = new byte[totalLength];
        result[0] = (byte)scheme;
        signature.CopyTo(result.AsSpan(SchemeFlagLengthBytes));
        rawPublicKey.CopyTo(result.AsSpan(SchemeFlagLengthBytes + signature.Length));
        return Base64.Encode(result);
    }

    /// <summary>
    /// Parses a serialized keypair signature (base64) into scheme, signature bytes, and public key bytes.
    /// </summary>
    public static (SignatureScheme Scheme, byte[] Signature, byte[] PublicKey) ParseSerializedKeypairSignature(string serializedSignature)
    {
        if (string.IsNullOrEmpty(serializedSignature))
        {
            throw new ArgumentNullException(nameof(serializedSignature));
        }

        byte[] bytes = Base64.Decode(serializedSignature.AsSpan());
        if (bytes.Length < MinSerializedSignatureLengthBytes)
        {
            throw new ArgumentException("Serialized signature too short.", nameof(serializedSignature));
        }

        byte flag = bytes[0];
        if (!Enum.IsDefined(typeof(SignatureScheme), (int)flag))
        {
            throw new ArgumentException($"Unsupported signature scheme flag: {flag}.", nameof(serializedSignature));
        }

        var scheme = (SignatureScheme)flag;
        int signatureSize = SignatureSchemeConstants.GetSignatureSize(scheme);
        int publicKeySize = SignatureSchemeConstants.GetPublicKeySize(scheme);
        int expectedLength = SchemeFlagLengthBytes + signatureSize + publicKeySize;
        if (bytes.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Invalid length for {scheme}: expected {expectedLength}, got {bytes.Length}.",
                nameof(serializedSignature));
        }

        byte[] signatureBytes = new byte[signatureSize];
        byte[] publicKeyBytes = new byte[publicKeySize];
        Buffer.BlockCopy(bytes, SchemeFlagLengthBytes, signatureBytes, 0, signatureSize);
        Buffer.BlockCopy(bytes, SchemeFlagLengthBytes + signatureSize, publicKeyBytes, 0, publicKeySize);
        return (scheme, signatureBytes, publicKeyBytes);
    }
}
