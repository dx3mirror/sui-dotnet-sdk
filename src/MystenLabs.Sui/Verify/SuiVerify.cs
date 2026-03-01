namespace MystenLabs.Sui.Verify;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Keypairs.Ed25519;
using MystenLabs.Sui.Keypairs.Secp256k1;
using MystenLabs.Sui.Keypairs.Secp256r1;
using MystenLabs.Sui.Multisig;
using MystenLabs.Sui.Transactions;
using MystenLabs.Sui.Utils;

/// <summary>
/// Verifies Sui signatures and reconstructs public keys from serialized signatures.
/// Supports Ed25519 single-signature verification; MultiSig and other schemes are not yet supported.
/// </summary>
public static class SuiVerify
{
    private const int MinSuiPublicKeyBytesLength = 2;

    /// <summary>
    /// Builds a public key from raw key bytes for the given signature scheme.
    /// Only Ed25519 is supported; other schemes throw <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="scheme">Signature scheme (only <see cref="SignatureScheme.Ed25519"/> supported).</param>
    /// <param name="bytes">Raw public key bytes (e.g. 32 bytes for Ed25519).</param>
    /// <param name="address">Optional address to assert; if provided, the key's Sui address must match.</param>
    /// <returns>The public key instance.</returns>
    public static PublicKey PublicKeyFromRawBytes(
        SignatureScheme scheme,
        ReadOnlySpan<byte> bytes,
        string? address = null)
    {
        PublicKey publicKey = scheme switch
        {
            SignatureScheme.Ed25519 => new Ed25519PublicKey(bytes.ToArray()),
            SignatureScheme.Secp256k1 => new Secp256k1PublicKey(bytes.ToArray()),
            SignatureScheme.Secp256r1 => new Secp256r1PublicKey(bytes.ToArray()),
            SignatureScheme.MultiSig => new MultiSigPublicKey(bytes.ToArray()),
            SignatureScheme.ZkLogin => throw new ArgumentException("ZkLogin is not yet supported for verification.", nameof(scheme)),
            SignatureScheme.Passkey => throw new ArgumentException("Passkey is not yet supported for verification.", nameof(scheme)),
            _ => throw new ArgumentException($"Unsupported signature scheme: {scheme}.", nameof(scheme))
        };

        if (!string.IsNullOrEmpty(address) && !publicKey.VerifyAddress(address))
        {
            throw new ArgumentException("Public key bytes do not match the provided address.", nameof(address));
        }

        return publicKey;
    }

    /// <summary>
    /// Builds a public key from Sui-format bytes (one byte scheme flag followed by raw key bytes).
    /// Accepts base64-encoded string or raw bytes.
    /// </summary>
    /// <param name="suiPublicKeyOrBase64">Base64-encoded Sui public key, or raw Sui bytes (flag + raw key).</param>
    /// <param name="address">Optional address to assert.</param>
    /// <returns>The public key instance.</returns>
    public static PublicKey PublicKeyFromSuiBytes(
        string suiPublicKeyOrBase64,
        string? address = null)
    {
        if (string.IsNullOrEmpty(suiPublicKeyOrBase64))
        {
            throw new ArgumentNullException(nameof(suiPublicKeyOrBase64));
        }

        byte[] bytes = Base64.Decode(suiPublicKeyOrBase64.AsSpan());
        return PublicKeyFromSuiBytes(bytes.AsSpan(), address);
    }

    /// <summary>
    /// Builds a public key from Sui-format bytes (one byte scheme flag followed by raw key bytes).
    /// </summary>
    /// <param name="suiBytes">Sui public key bytes (flag + raw key).</param>
    /// <param name="address">Optional address to assert.</param>
    /// <returns>The public key instance.</returns>
    public static PublicKey PublicKeyFromSuiBytes(
        ReadOnlySpan<byte> suiBytes,
        string? address = null)
    {
        if (suiBytes.Length < MinSuiPublicKeyBytesLength)
        {
            throw new ArgumentException("Sui public key bytes too short (expected flag + raw key).", nameof(suiBytes));
        }

        byte flag = suiBytes[0];
        if (!Enum.IsDefined(typeof(SignatureScheme), (int)flag))
        {
            throw new ArgumentException($"Unsupported signature scheme flag: {flag}.", nameof(suiBytes));
        }

        var scheme = (SignatureScheme)flag;
        ReadOnlySpan<byte> rawKey = suiBytes.Slice(1);
        return PublicKeyFromRawBytes(scheme, rawKey, address);
    }

    /// <summary>
    /// Verifies a serialized signature over the given message digest and optionally checks the signer address.
    /// </summary>
    /// <param name="data">Message digest (e.g. 32-byte Blake2b hash).</param>
    /// <param name="serializedSignature">Base64 serialized signature (flag + signature + public key).</param>
    /// <param name="address">Optional address; if set, the recovered public key's Sui address must match.</param>
    /// <param name="cancellationToken">Optional cancellation (unused; for API consistency).</param>
    /// <returns>The public key that produced the signature.</returns>
    /// <exception cref="InvalidOperationException">Signature is invalid for the provided data.</exception>
    public static Task<PublicKey> VerifySignatureAsync(
        ReadOnlySpan<byte> data,
        string serializedSignature,
        string? address = null,
        CancellationToken cancellationToken = default)
    {
        byte[] dataCopy = data.ToArray();
        MultiSigStruct? multisig = MultiSigSignature.ParseSerialized(serializedSignature);
        if (multisig != null)
        {
            var multisigPk = new MultiSigPublicKey(multisig.MultisigPk);
            bool valid = multisigPk.VerifyAsync(dataCopy, serializedSignature).GetAwaiter().GetResult();
            if (!valid)
            {
                throw new InvalidOperationException("Signature is not valid for the provided data.");
            }

            if (!string.IsNullOrEmpty(address) && !multisigPk.VerifyAddress(address))
            {
                throw new InvalidOperationException("Signature is not valid for the provided address.");
            }

            return Task.FromResult<PublicKey>(multisigPk);
        }

        (PublicKey publicKey, byte[] signatureBytes) = ParseSerializedSignatureToKeyAndSig(serializedSignature);

        if (!publicKey.Verify(dataCopy, signatureBytes))
        {
            throw new InvalidOperationException("Signature is not valid for the provided data.");
        }

        if (!string.IsNullOrEmpty(address) && !publicKey.VerifyAddress(address))
        {
            throw new InvalidOperationException("Signature is not valid for the provided address.");
        }

        return Task.FromResult(publicKey);
    }

    /// <summary>
    /// Verifies a serialized signature over a personal message (intent-wrapped, then hashed).
    /// </summary>
    /// <param name="message">Raw message bytes (before intent).</param>
    /// <param name="serializedSignature">Base64 serialized signature.</param>
    /// <param name="address">Optional address to check.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The public key that produced the signature.</returns>
    public static Task<PublicKey> VerifyPersonalMessageSignatureAsync(
        ReadOnlySpan<byte> message,
        string serializedSignature,
        string? address = null,
        CancellationToken cancellationToken = default)
    {
        byte[] intentMessage = Intent.MessageWithIntent(IntentScope.PersonalMessage, message);
        byte[] digest = Blake2b.Hash256(intentMessage);
        return VerifySignatureAsync(digest, serializedSignature, address, cancellationToken);
    }

    /// <summary>
    /// Verifies a serialized signature over transaction bytes (digest = TransactionData:: + bytes, then verify).
    /// </summary>
    /// <param name="transactionBytes">BCS-serialized transaction data (e.g. from <see cref="TransactionDataBuilder.SerializeToBcs"/>).</param>
    /// <param name="serializedSignature">Base64 serialized signature.</param>
    /// <param name="address">Optional address to check.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The public key that produced the signature.</returns>
    public static Task<PublicKey> VerifyTransactionSignatureAsync(
        ReadOnlySpan<byte> transactionBytes,
        string serializedSignature,
        string? address = null,
        CancellationToken cancellationToken = default)
    {
        byte[] digest = TransactionHasher.GetDigestToSign(transactionBytes);
        return VerifySignatureAsync(digest, serializedSignature, address, cancellationToken);
    }

    private static (PublicKey PublicKey, byte[] SignatureBytes) ParseSerializedSignatureToKeyAndSig(string serializedSignature)
    {
        (SignatureScheme scheme, byte[] signatureBytes, byte[] publicKeyBytes) =
            Signature.ParseSerializedKeypairSignature(serializedSignature);

        PublicKey publicKey = PublicKeyFromRawBytes(scheme, publicKeyBytes);
        return (publicKey, signatureBytes);
    }
}
