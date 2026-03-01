namespace MystenLabs.Sui.Multisig;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Utils;

/// <summary>
/// Signer that combines multiple signers into a MultiSig. Does not support direct Sign(digest); use SignTransaction or SignPersonalMessage.
/// </summary>
public sealed class MultiSigSigner : Signer
{
    private readonly MultiSigPublicKey _publicKey;
    private readonly IReadOnlyList<Signer> _signers;

    /// <summary>
    /// Creates a MultiSig signer from the multisig public key and the signers that will contribute (must be a subset of the multisig's keys with combined weight >= threshold).
    /// </summary>
    public MultiSigSigner(MultiSigPublicKey publicKey, IReadOnlyList<Signer>? signers = null)
    {
        _publicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        _signers = signers ?? [];

        var weightsByAddress = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (MultiSigPublicKeyEntry entry in _publicKey.GetPublicKeys())
        {
            weightsByAddress[entry.PublicKey.ToSuiAddress()] = entry.Weight;
        }

        int combinedWeight = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (Signer signer in _signers)
        {
            string address = signer.ToSuiAddress();
            if (!seen.Add(address))
            {
                throw new ArgumentException("Can't create MultiSigSigner with duplicate signers.", nameof(signers));
            }

            if (!weightsByAddress.TryGetValue(address, out int weight))
            {
                throw new ArgumentException($"Signer {address} is not part of the MultiSig public key.", nameof(signers));
            }

            combinedWeight += weight;
        }

        if (combinedWeight < _publicKey.GetThreshold())
        {
            throw new ArgumentException("Combined weight of signers is less than threshold.", nameof(signers));
        }
    }

    /// <inheritdoc />
    public override SignatureScheme GetKeyScheme() => SignatureScheme.MultiSig;

    /// <inheritdoc />
    public override PublicKey GetPublicKey() => _publicKey;

    /// <inheritdoc />
    public override byte[] Sign(ReadOnlySpan<byte> digest)
    {
        throw new InvalidOperationException(
            "MultiSigSigner does not support signing directly. Use SignTransaction or SignPersonalMessage instead.");
    }

    /// <inheritdoc />
    public override sealed SignatureWithBytes SignTransaction(ReadOnlySpan<byte> transactionBytes)
    {
        var signatures = new List<string>();
        foreach (Signer signer in _signers)
        {
            SignatureWithBytes result = signer.SignTransaction(transactionBytes);
            signatures.Add(result.Signature);
        }

        string combined = _publicKey.CombinePartialSignatures(signatures);
        string? bytesBase64 = null;
        if (!transactionBytes.IsEmpty)
        {
            bytesBase64 = Base64.Encode(transactionBytes);
        }

        return new SignatureWithBytes(combined, bytesBase64);
    }

    /// <inheritdoc />
    public override sealed SignatureWithBytes SignWithIntent(ReadOnlySpan<byte> bytes, IntentScope intent)
    {
        var signatures = new List<string>();
        foreach (Signer signer in _signers)
        {
            SignatureWithBytes result = signer.SignWithIntent(bytes, intent);
            signatures.Add(result.Signature);
        }

        string combined = _publicKey.CombinePartialSignatures(signatures);
        string? bytesBase64 = null;
        if (!bytes.IsEmpty)
        {
            bytesBase64 = Base64.Encode(bytes);
        }

        return new SignatureWithBytes(combined, bytesBase64);
    }
}
