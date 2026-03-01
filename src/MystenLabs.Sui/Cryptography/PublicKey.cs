namespace MystenLabs.Sui.Cryptography;

using MystenLabs.Sui.Utils;

/// <summary>
/// Base type for Sui public keys. Provides raw bytes, Sui bytes (flag + raw), Sui address, and verification.
/// </summary>
public abstract class PublicKey
{
    /// <summary>
    /// Raw public key bytes (without scheme flag).
    /// </summary>
    public abstract byte[] ToRawBytes();

    /// <summary>
    /// Signature scheme flag byte used in Sui serialization.
    /// </summary>
    public abstract byte Flag();

    /// <summary>
    /// Bytes used for Sui address and serialization: one byte scheme flag followed by raw key bytes.
    /// </summary>
    public byte[] ToSuiBytes()
    {
        byte[] raw = ToRawBytes();
        var result = new byte[raw.Length + 1];
        result[0] = Flag();
        Buffer.BlockCopy(raw, 0, result, 1, raw.Length);
        return result;
    }

    /// <summary>
    /// Sui address derived from this public key (Blake2b hash of ToSuiBytes(), first 32 bytes as 0x + 64 hex).
    /// </summary>
    public virtual string ToSuiAddress()
    {
        byte[] suiBytes = ToSuiBytes();
        byte[] hash = Blake2b.Hash256(suiBytes);
        return SuiAddress.Normalize(Hex.Encode(hash));
    }

    /// <summary>
    /// Base64-encoded Sui public key (flag + raw bytes).
    /// </summary>
    public string ToSuiPublicKey()
    {
        return Base64.Encode(ToSuiBytes());
    }

    /// <summary>
    /// Verifies a signature over the given message (raw digest).
    /// </summary>
    /// <param name="data">Message digest (e.g. 32 bytes).</param>
    /// <param name="signature">Signature bytes or base64 serialized signature (flag + signature + public key).</param>
    /// <returns>True if the signature is valid.</returns>
    public abstract bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature);

    /// <summary>
    /// Verifies a signature over the given message with intent (hashes intent message then verifies).
    /// </summary>
    public bool VerifyWithIntent(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> signature, IntentScope intent)
    {
        byte[] intentMessage = Intent.MessageWithIntent(intent, bytes);
        byte[] digest = Blake2b.Hash256(intentMessage);
        return Verify(digest, signature);
    }

    /// <summary>
    /// Verifies that this key's address matches the given address.
    /// </summary>
    public bool VerifyAddress(string address)
    {
        return ToSuiAddress() == SuiAddress.Normalize(address.AsSpan());
    }

    /// <summary>
    /// Returns true if this key has the same raw bytes as the other.
    /// </summary>
    public bool Equals(PublicKey other)
    {
        if (other == null)
        {
            return false;
        }

        byte[] thisBytes = ToRawBytes();
        byte[] otherBytes = other.ToRawBytes();
        if (thisBytes.Length != otherBytes.Length)
        {
            return false;
        }

        for (int index = 0; index < thisBytes.Length; index++)
        {
            if (thisBytes[index] != otherBytes[index])
            {
                return false;
            }
        }

        return true;
    }
}
