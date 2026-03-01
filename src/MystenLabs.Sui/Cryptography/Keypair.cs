namespace MystenLabs.Sui.Cryptography;

/// <summary>
/// Sui private key size in bytes (seed/secret key length).
/// </summary>
public static class KeypairConstants
{
    public const int PrivateKeySize = 32;
}

/// <summary>
/// Result of signing with intent: serialized signature and optional message bytes (base64).
/// </summary>
public sealed class SignatureWithBytes
{
    /// <summary>
    /// Base64 serialized signature (flag + signature + public key).
    /// </summary>
    public string Signature { get; }

    /// <summary>
    /// Optional base64-encoded bytes that were signed.
    /// </summary>
    public string? Bytes { get; }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public SignatureWithBytes(string signature, string? bytes = null)
    {
        Signature = signature ?? throw new ArgumentNullException(nameof(signature));
        Bytes = bytes;
    }
}

/// <summary>
/// Signer capable of signing digests and intent-wrapped messages. Thread-safe usage depends on the implementation.
/// </summary>
public abstract class Signer
{
    /// <summary>
    /// Signs the given digest (e.g. 32-byte Blake2b hash).
    /// </summary>
    /// <param name="digest">Message digest to sign.</param>
    /// <returns>Signature bytes.</returns>
    public abstract byte[] Sign(ReadOnlySpan<byte> digest);

    /// <summary>
    /// Signs the message with the given intent (builds intent message, hashes with Blake2b, then signs).
    /// </summary>
    public virtual SignatureWithBytes SignWithIntent(ReadOnlySpan<byte> bytes, IntentScope intent)
    {
        byte[] intentMessage = Intent.MessageWithIntent(intent, bytes);
        byte[] digest = Blake2b.Hash256(intentMessage);
        byte[] signature = Sign(digest);
        string serialized = Signature.ToSerializedSignature(GetKeyScheme(), signature, GetPublicKey());
        string? bytesBase64 = null;
        if (!bytes.IsEmpty)
        {
            bytesBase64 = MystenLabs.Sui.Utils.Base64.Encode(bytes);
        }

        return new SignatureWithBytes(serialized, bytesBase64);
    }

    /// <summary>
    /// Signs transaction bytes with TransactionData intent.
    /// </summary>
    public virtual SignatureWithBytes SignTransaction(ReadOnlySpan<byte> transactionBytes)
    {
        return SignWithIntent(transactionBytes, IntentScope.TransactionData);
    }

    /// <summary>
    /// Sui address for this signer (derived from public key).
    /// </summary>
    public string ToSuiAddress()
    {
        return GetPublicKey().ToSuiAddress();
    }

    /// <summary>
    /// Signature scheme of this signer.
    /// </summary>
    public abstract SignatureScheme GetKeyScheme();

    /// <summary>
    /// Public key for this signer.
    /// </summary>
    public abstract PublicKey GetPublicKey();
}

/// <summary>
/// Keypair that holds a secret key and can produce signatures. Extends <see cref="Signer"/> with secret key export.
/// </summary>
public abstract class Keypair : Signer
{
    /// <summary>
    /// Returns the secret key in Sui Bech32 format (suiprivkey...) or raw bytes representation as configured.
    /// </summary>
    public abstract string GetSecretKey();
}
