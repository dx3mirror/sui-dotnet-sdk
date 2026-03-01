namespace MystenLabs.Sui.Multisig;

using System.Collections.ObjectModel;
using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Utils;
using MystenLabs.Sui.Verify;

/// <summary>
/// Maximum number of signers allowed in a multisig.
/// </summary>
public static class MultiSigConstants
{
    public const int MaxSignerInMultisig = 10;

    public const int MinSignerInMultisig = 1;

    /// <summary>
    /// Maximum bitmap value (2^10 - 1) for up to 10 signers.
    /// </summary>
    public const int MaxBitmapValue = 1024;
}

/// <summary>
/// One parsed partial signature from a multisig: scheme, signature bytes, public key, and weight.
/// </summary>
/// <param name="SignatureScheme">Scheme used for this signature.</param>
/// <param name="Signature">Raw signature bytes.</param>
/// <param name="PublicKey">Recovered public key.</param>
/// <param name="Weight">Weight of this key in the multisig.</param>
public sealed record ParsedPartialMultiSigSignature(
    SignatureScheme SignatureScheme,
    byte[] Signature,
    PublicKey PublicKey,
    int Weight);

/// <summary>
/// MultiSig public key: multiple public keys with weights and a threshold. Supports Ed25519 keys; other schemes in BCS are parsed but verification supports only Ed25519.
/// </summary>
public sealed class MultiSigPublicKey : PublicKey
{
    private const int SchemeFlagLengthBytes = 1;
    private const int MaxSinglePublicKeyRawBytes = 64;
    private const int MaxPublicKeySuiBytesPerEntry = SchemeFlagLengthBytes + MaxSinglePublicKeyRawBytes;
    private const int U16BcsLengthBytes = 2;
    private static readonly byte MultiSigSchemeFlagByte = (byte)SignatureScheme.MultiSig;

    private readonly byte[] _rawBytes;
    private readonly MultiSigPublicKeyStruct _multisigPk;
    private readonly ReadOnlyCollection<MultiSigPublicKeyEntry> _publicKeys;

    /// <summary>
    /// Creates a MultiSig public key from BCS bytes (or base64 string) or from a struct.
    /// </summary>
    /// <param name="value">Base64 string, raw BCS bytes, or a <see cref="MultiSigPublicKeyStruct"/>.</param>
    public MultiSigPublicKey(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException(nameof(value));
        }

        byte[] bytes = MystenLabs.Sui.Utils.Base64.Decode(value.AsSpan());
        _rawBytes = bytes;
        _multisigPk = MultiSigBcs.ParseMultiSigPublicKey(_rawBytes);
        _publicKeys = BuildPublicKeysList(_multisigPk);
    }

    /// <summary>
    /// Creates a MultiSig public key from raw BCS bytes.
    /// </summary>
    public MultiSigPublicKey(byte[] value)
    {
        _rawBytes = value ?? throw new ArgumentNullException(nameof(value));
        _multisigPk = MultiSigBcs.ParseMultiSigPublicKey(_rawBytes);
        _publicKeys = BuildPublicKeysList(_multisigPk);
    }

    /// <summary>
    /// Creates a MultiSig public key from a struct (e.g. from <see cref="FromPublicKeys"/>).
    /// </summary>
    public MultiSigPublicKey(MultiSigPublicKeyStruct value)
    {
        _multisigPk = value ?? throw new ArgumentNullException(nameof(value));
        _rawBytes = MultiSigBcs.SerializeMultiSigPublicKey(_multisigPk);
        _publicKeys = BuildPublicKeysList(_multisigPk);
    }

    /// <summary>
    /// Builds a MultiSig public key from a set of public keys and their weights, with a threshold.
    /// Only Ed25519 is supported for creation; other schemes throw.
    /// </summary>
    public static MultiSigPublicKey FromPublicKeys(int threshold, IReadOnlyList<(PublicKey PublicKey, int Weight)> publicKeys)
    {
        if (publicKeys == null || publicKeys.Count == 0)
        {
            throw new ArgumentException("At least one public key is required.", nameof(publicKeys));
        }

        if (threshold < 1)
        {
            throw new ArgumentException("Invalid threshold.", nameof(threshold));
        }

        var pkMap = new List<MultiSigPkMapEntry>();
        foreach ((PublicKey publicKey, int weight) in publicKeys)
        {
            if (weight < 1)
            {
                throw new ArgumentException("Invalid weight.", nameof(publicKeys));
            }

            byte flag = publicKey.Flag();
            if (!Enum.IsDefined(typeof(SignatureScheme), (int)flag))
            {
                throw new ArgumentException($"Unsupported signature scheme flag: {flag}.", nameof(publicKeys));
            }

            var scheme = (SignatureScheme)flag;
            byte[] rawBytes = publicKey.ToRawBytes();
            pkMap.Add(new MultiSigPkMapEntry(scheme, rawBytes, (byte)weight));
        }

        if (publicKeys.Count > MultiSigConstants.MaxSignerInMultisig)
        {
            throw new ArgumentException($"Max number of signers in a multisig is {MultiSigConstants.MaxSignerInMultisig}.", nameof(publicKeys));
        }

        int totalWeight = publicKeys.Sum(entry => entry.Weight);
        if (threshold > totalWeight)
        {
            throw new ArgumentException("Unreachable threshold.", nameof(threshold));
        }

        var struct_ = new MultiSigPublicKeyStruct(pkMap, (ushort)threshold);
        return new MultiSigPublicKey(struct_);
    }

    /// <inheritdoc />
    public override byte[] ToRawBytes()
    {
        return (byte[])_rawBytes.Clone();
    }

    /// <inheritdoc />
    public override byte Flag()
    {
        return (byte)SignatureScheme.MultiSig;
    }

    /// <inheritdoc />
    public override bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        string serialized = signature.Length > 0
            ? System.Text.Encoding.UTF8.GetString(signature.ToArray())
            : string.Empty;
        return VerifyAsync(data.Length > 0 ? data.ToArray() : [], serialized).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Verifies a serialized MultiSig signature against the given message digest.
    /// </summary>
    public Task<bool> VerifyAsync(byte[] message, string serializedMultisigSignature)
    {
        MultiSigStruct? multisig = MultiSigSignature.ParseSerialized(serializedMultisigSignature);
        if (multisig == null)
        {
            return Task.FromResult(false);
        }

        byte[] ourPkBytes = MultiSigBcs.SerializeMultiSigPublicKey(_multisigPk);
        byte[] theirPkBytes = MultiSigBcs.SerializeMultiSigPublicKey(multisig.MultisigPk);
        if (ourPkBytes.Length != theirPkBytes.Length || !ourPkBytes.AsSpan().SequenceEqual(theirPkBytes))
        {
            return Task.FromResult(false);
        }

        IReadOnlyList<ParsedPartialMultiSigSignature> partials = ParsePartialSignatures(multisig);
        int signatureWeight = 0;
        foreach (ParsedPartialMultiSigSignature partial in partials)
        {
            if (!partial.PublicKey.Verify(message, partial.Signature))
            {
                return Task.FromResult(false);
            }

            signatureWeight += partial.Weight;
        }

        return Task.FromResult(signatureWeight >= _multisigPk.Threshold);
    }

    /// <summary>
    /// Returns the list of public keys and their weights.
    /// </summary>
    public IReadOnlyList<MultiSigPublicKeyEntry> GetPublicKeys() => _publicKeys;

    /// <summary>
    /// Returns the threshold (minimum combined weight required).
    /// </summary>
    public int GetThreshold() => _multisigPk.Threshold;

    /// <summary>
    /// Combines multiple serialized partial signatures into one MultiSig serialized signature.
    /// Each signer must be in this multisig and sign at most once; combined weight must meet the threshold.
    /// </summary>
    public string CombinePartialSignatures(IReadOnlyList<string> serializedSignatures)
    {
        if (serializedSignatures == null || serializedSignatures.Count == 0)
        {
            throw new ArgumentException("At least one signature is required.", nameof(serializedSignatures));
        }

        if (serializedSignatures.Count > MultiSigConstants.MaxSignerInMultisig)
        {
            throw new ArgumentException($"Max number of signatures in a multisig is {MultiSigConstants.MaxSignerInMultisig}.", nameof(serializedSignatures));
        }

        int bitmap = 0;
        var compressedSigs = new List<CompressedSignatureEntry>();

        for (int index = 0; index < serializedSignatures.Count; index++)
        {
            string serializedSignature = serializedSignatures[index];
            (SignatureScheme scheme, byte[] signatureBytes, byte[] publicKeyBytes) = Signature.ParseSerializedKeypairSignature(serializedSignature);

            if (scheme == SignatureScheme.MultiSig)
            {
                throw new InvalidOperationException("MultiSig is not supported inside MultiSig.");
            }

            int publicKeyIndex = -1;
            for (int keyIndex = 0; keyIndex < _publicKeys.Count; keyIndex++)
            {
                if (_publicKeys[keyIndex].PublicKey.ToRawBytes().AsSpan().SequenceEqual(publicKeyBytes))
                {
                    if ((bitmap & (1 << keyIndex)) != 0)
                    {
                        throw new InvalidOperationException("Received multiple signatures from the same public key.");
                    }

                    publicKeyIndex = keyIndex;
                    break;
                }
            }

            if (publicKeyIndex < 0)
            {
                throw new InvalidOperationException("Received signature from unknown public key.");
            }

            bitmap |= 1 << publicKeyIndex;
            compressedSigs.Add(new CompressedSignatureEntry(scheme, signatureBytes));
        }

        var multisig = new MultiSigStruct(compressedSigs, (ushort)bitmap, _multisigPk);
        byte[] multisigBytes = MultiSigBcs.SerializeMultiSig(multisig);
        var result = new byte[SchemeFlagLengthBytes + multisigBytes.Length];
        result[0] = MultiSigSchemeFlagByte;
        multisigBytes.CopyTo(result, SchemeFlagLengthBytes);
        return MystenLabs.Sui.Utils.Base64.Encode(result);
    }

    /// <inheritdoc />
    public override sealed string ToSuiAddress()
    {
        int maxLength = SchemeFlagLengthBytes + U16BcsLengthBytes + (MaxPublicKeySuiBytesPerEntry + SchemeFlagLengthBytes) * MultiSigConstants.MaxSignerInMultisig;
        byte[] addressHashInput = new byte[maxLength];
        int position = 0;
        addressHashInput[position++] = MultiSigSchemeFlagByte;

        var thresholdWriter = new BcsWriter();
        thresholdWriter.WriteU16(_multisigPk.Threshold);
        byte[] thresholdBytes = thresholdWriter.ToBytes();
        foreach (byte currentByte in thresholdBytes)
        {
            addressHashInput[position++] = currentByte;
        }

        foreach (MultiSigPublicKeyEntry entry in _publicKeys)
        {
            byte[] suiBytes = entry.PublicKey.ToSuiBytes();
            foreach (byte currentByte in suiBytes)
            {
                addressHashInput[position++] = currentByte;
            }

            addressHashInput[position++] = (byte)entry.Weight;
        }

        byte[] hash = Blake2b.Hash256(addressHashInput.AsSpan(0, position));
        return SuiAddress.Normalize(Hex.Encode(hash));
    }

    /// <summary>
    /// Parses a MultiSig struct into individual partial signatures with public keys and weights.
    /// </summary>
    public static IReadOnlyList<ParsedPartialMultiSigSignature> ParsePartialSignatures(MultiSigStruct multisig)
    {
        int[] indices = GetBitmapIndices(multisig.Bitmap);
        if (indices.Length != multisig.Sigs.Count)
        {
            throw new InvalidOperationException("Bitmap and sigs length mismatch.");
        }

        IReadOnlyList<MultiSigPkMapEntry> publicKeyMap = multisig.MultisigPk.PkMap;
        var result = new List<ParsedPartialMultiSigSignature>();
        for (int index = 0; index < multisig.Sigs.Count; index++)
        {
            int publicKeyIndex = indices[index];
            CompressedSignatureEntry signatureEntry = multisig.Sigs[index];
            if (signatureEntry.Scheme == SignatureScheme.MultiSig)
            {
                throw new InvalidOperationException("MultiSig is not supported inside MultiSig.");
            }

            MultiSigPkMapEntry pair = publicKeyMap[publicKeyIndex];
            PublicKey publicKey = SuiVerify.PublicKeyFromRawBytes(pair.Scheme, pair.PublicKeyBytes);
            result.Add(new ParsedPartialMultiSigSignature(signatureEntry.Scheme, signatureEntry.SignatureBytes, publicKey, pair.Weight));
        }

        return result;
    }

    private static int[] GetBitmapIndices(ushort bitmap)
    {
        if (bitmap < 0 || bitmap > MultiSigConstants.MaxBitmapValue)
        {
            throw new ArgumentException("Invalid bitmap.", nameof(bitmap));
        }

        var indices = new List<int>();
        for (int index = 0; index < MultiSigConstants.MaxSignerInMultisig; index++)
        {
            if ((bitmap & (1 << index)) != 0)
            {
                indices.Add(index);
            }
        }

        return indices.ToArray();
    }

    private static ReadOnlyCollection<MultiSigPublicKeyEntry> BuildPublicKeysList(MultiSigPublicKeyStruct multisigPk)
    {
        if (multisigPk.Threshold < 1)
        {
            throw new ArgumentException("Invalid threshold.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<MultiSigPublicKeyEntry>();

        foreach (MultiSigPkMapEntry entry in multisigPk.PkMap)
        {
            string key = Convert.ToHexString(entry.PublicKeyBytes);
            if (!seen.Add(key))
            {
                throw new ArgumentException("Multisig does not support duplicate public keys.");
            }

            if (entry.Weight < 1)
            {
                throw new ArgumentException("Invalid weight.");
            }

            PublicKey publicKey = SuiVerify.PublicKeyFromRawBytes(entry.Scheme, entry.PublicKeyBytes);
            list.Add(new MultiSigPublicKeyEntry(publicKey, entry.Weight));
        }

        int totalWeight = list.Sum(e => e.Weight);
        if (multisigPk.Threshold > totalWeight)
        {
            throw new ArgumentException("Unreachable threshold.");
        }

        if (list.Count > MultiSigConstants.MaxSignerInMultisig)
        {
            throw new ArgumentException($"Max number of signers in a multisig is {MultiSigConstants.MaxSignerInMultisig}.");
        }

        if (list.Count < MultiSigConstants.MinSignerInMultisig)
        {
            throw new ArgumentException($"Min number of signers in a multisig is {MultiSigConstants.MinSignerInMultisig}.");
        }

        return list.AsReadOnly();
    }
}

/// <summary>
/// One public key and its weight in a MultiSig.
/// </summary>
/// <param name="PublicKey">The public key.</param>
/// <param name="Weight">Weight for threshold.</param>
public sealed record MultiSigPublicKeyEntry(PublicKey PublicKey, int Weight);
