namespace MystenLabs.Sui.Keypairs.Ed25519;

using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MystenLabs.Sui.Utils;

/// <summary>
/// SLIP-0010 Ed25519 HD key derivation from a seed. Used to derive a 32-byte secret key and chain code from a path.
/// </summary>
public static class Ed25519HdKey
{
    private static readonly byte[] Ed25519CurveUtf8 = System.Text.Encoding.UTF8.GetBytes("ed25519 seed");
    private const uint HardenedOffset = 0x80000000;
    private const int KeyLengthBytes = 32;
    private const int ChainCodeLengthBytes = 32;
    private const int HmacSha512OutputLengthBytes = 64;

    /// <summary>
    /// Result of Ed25519 HD derivation: secret key and chain code.
    /// </summary>
    public sealed class DerivedKeys
    {
        /// <summary>
        /// 32-byte secret key (seed for Ed25519 keypair).
        /// </summary>
        public byte[] Key { get; }

        /// <summary>
        /// 32-byte chain code for further derivation.
        /// </summary>
        public byte[] ChainCode { get; }

        internal DerivedKeys(byte[] key, byte[] chainCode)
        {
            Key = key;
            ChainCode = chainCode;
        }
    }

    /// <summary>
    /// Derives key and chain code for the given path from a hex seed.
    /// </summary>
    /// <param name="path">SLIP-0010 path (e.g. m/44'/784'/0'/0'/0'); all segments must be hardened.</param>
    /// <param name="seedHex">64-byte seed as hex string (e.g. from BIP39 mnemonic).</param>
    /// <param name="hardenedOffset">Offset added to each path segment (default 0x80000000 for hardened).</param>
    /// <returns>Derived 32-byte key and 32-byte chain code.</returns>
    public static DerivedKeys DerivePath(string path, string seedHex, uint hardenedOffset = HardenedOffset)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Invalid derivation path.", nameof(path));
        }

        if (string.IsNullOrWhiteSpace(seedHex))
        {
            throw new ArgumentException("Seed cannot be null or empty.", nameof(seedHex));
        }

        if (!IsValidDerivationPath(path))
        {
            throw new ArgumentException("Invalid derivation path.", nameof(path));
        }

        byte[] seed = Hex.Decode(seedHex.AsSpan());
        if (seed.Length != 64)
        {
            throw new ArgumentException("Seed must be 64 bytes (128 hex chars).", nameof(seedHex));
        }

        (byte[] key, byte[] chainCode) = GetMasterKeyFromSeed(seed);

        string[] segments = path.Split('/');
        for (int index = 1; index < segments.Length; index++)
        {
            string segment = segments[index].Replace("'", string.Empty, StringComparison.Ordinal);
            if (!int.TryParse(segment, System.Globalization.NumberStyles.None, null, out int segmentValue))
            {
                throw new ArgumentException("Invalid derivation path: segment is not a number.", nameof(path));
            }

            uint indexWithOffset = (uint)segmentValue + hardenedOffset;
            (key, chainCode) = CkdPriv(key, chainCode, indexWithOffset);
        }

        return new DerivedKeys(key, chainCode);
    }

    private static bool IsValidDerivationPath(string path)
    {
        if (!s_derivationPathRegex.IsMatch(path))
        {
            return false;
        }

        string[] segments = path.Split('/');
        for (int index = 1; index < segments.Length; index++)
        {
            string segment = segments[index].Replace("'", string.Empty, StringComparison.Ordinal);
            if (!int.TryParse(segment, System.Globalization.NumberStyles.None, null, out _))
            {
                return false;
            }
        }

        return true;
    }

    private static readonly Regex s_derivationPathRegex = new(@"^m(/[0-9]+')+$");

    private static (byte[] Key, byte[] ChainCode) GetMasterKeyFromSeed(byte[] seed)
    {
        byte[] i = HmacSha512(Ed25519CurveUtf8, seed);
        byte[] key = new byte[KeyLengthBytes];
        byte[] chainCode = new byte[ChainCodeLengthBytes];
        Buffer.BlockCopy(i, 0, key, 0, KeyLengthBytes);
        Buffer.BlockCopy(i, KeyLengthBytes, chainCode, 0, ChainCodeLengthBytes);
        return (key, chainCode);
    }

    private static (byte[] Key, byte[] ChainCode) CkdPriv(byte[] key, byte[] chainCode, uint index)
    {
        byte[] indexBigEndian = new byte[sizeof(uint)];
        indexBigEndian[0] = (byte)(index >> 24);
        indexBigEndian[1] = (byte)(index >> 16);
        indexBigEndian[2] = (byte)(index >> 8);
        indexBigEndian[3] = (byte)index;

        byte[] data = new byte[1 + key.Length + indexBigEndian.Length];
        data[0] = 0;
        Buffer.BlockCopy(key, 0, data, 1, key.Length);
        Buffer.BlockCopy(indexBigEndian, 0, data, 1 + key.Length, indexBigEndian.Length);

        byte[] i = HmacSha512(chainCode, data);
        byte[] newKey = new byte[KeyLengthBytes];
        byte[] newChainCode = new byte[ChainCodeLengthBytes];
        Buffer.BlockCopy(i, 0, newKey, 0, KeyLengthBytes);
        Buffer.BlockCopy(i, KeyLengthBytes, newChainCode, 0, ChainCodeLengthBytes);
        return (newKey, newChainCode);
    }

    private static byte[] HmacSha512(byte[] key, byte[] data)
    {
        byte[] result;
        using (var hmac = new HMACSHA512(key))
        {
            result = hmac.ComputeHash(data);
        }

        if (result.Length != HmacSha512OutputLengthBytes)
        {
            throw new InvalidOperationException("HMAC-SHA512 must produce 64 bytes.");
        }

        return result;
    }
}
