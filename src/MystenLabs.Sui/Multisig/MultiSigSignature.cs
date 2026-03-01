namespace MystenLabs.Sui.Multisig;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Utils;

/// <summary>
/// Parses serialized MultiSig signatures (base64: flag 0x03 + BCS MultiSig bytes).
/// </summary>
public static class MultiSigSignature
{
    private const int MinSerializedSignatureLengthBytes = 2;

    /// <summary>
    /// Parses a base64-encoded serialized signature. Returns the MultiSig struct if the first byte is the MultiSig flag (0x03); otherwise null.
    /// </summary>
    /// <param name="serializedSignature">Base64 serialized signature.</param>
    /// <returns>The parsed MultiSig struct, or null if not a MultiSig signature.</returns>
    public static MultiSigStruct? ParseSerialized(string? serializedSignature)
    {
        if (string.IsNullOrEmpty(serializedSignature))
        {
            return null;
        }

        byte[] bytes = Base64.Decode(serializedSignature.AsSpan());
        if (bytes.Length < MinSerializedSignatureLengthBytes || bytes[0] != (byte)SignatureScheme.MultiSig)
        {
            return null;
        }

        return MultiSigBcs.ParseMultiSig(bytes.AsSpan(1));
    }
}
