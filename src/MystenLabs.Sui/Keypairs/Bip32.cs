namespace MystenLabs.Sui.Keypairs;

using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Math;
using MystenLabs.Sui.Keypairs.Secp256k1;

/// <summary>
/// BIP-32 HD key derivation (Secp256k1 curve). Used to derive a 32-byte private key from a seed and path.
/// Sui uses m/54'/784'/... for Secp256k1 and m/74'/784'/... for Secp256r1 (same derivation, use private key only).
/// </summary>
internal static class Bip32
{
    private static readonly byte[] BitcoinSeedUtf8 = System.Text.Encoding.UTF8.GetBytes("Bitcoin seed");
    private const uint HardenedOffset = 0x80000000;
    private const int KeyLengthBytes = 32;
    private const int ChainCodeLengthBytes = 32;
    private const int HmacSha512OutputLengthBytes = 64;
    private const int CompressedPublicKeyLengthBytes = 33;

    private static readonly Regex PathRegex = new(@"^m(/\d+'?)+$");

    /// <summary>
    /// Derives a 32-byte private key for the given BIP-32 path from a 64-byte seed.
    /// </summary>
    /// <param name="seed">64-byte BIP39 seed.</param>
    /// <param name="path">Path (e.g. m/54'/784'/0'/0/0 or m/74'/784'/0'/0/0).</param>
    /// <returns>32-byte private key.</returns>
    public static byte[] DerivePrivateKey(byte[] seed, string path)
    {
        if (seed == null || seed.Length != 64)
        {
            throw new ArgumentException("Seed must be 64 bytes.", nameof(seed));
        }

        if (string.IsNullOrWhiteSpace(path) || !PathRegex.IsMatch(path))
        {
            throw new ArgumentException("Invalid derivation path.", nameof(path));
        }

        string[] segments = path.Split('/');
        if (segments.Length < 2 || segments[0] != "m")
        {
            throw new ArgumentException("Invalid derivation path.", nameof(path));
        }

        (byte[] key, byte[] chainCode) = GetMasterKeyFromSeed(seed);
        BigInteger curveOrder = Secp256k1Impl.GetCurveOrder();

        for (int index = 1; index < segments.Length; index++)
        {
            string segment = segments[index];
            bool hardened = segment.EndsWith("'", StringComparison.Ordinal);
            string numberPart = hardened ? segment[..^1] : segment;
            if (!uint.TryParse(numberPart, System.Globalization.NumberStyles.None, null, out uint segmentValue))
            {
                throw new ArgumentException($"Invalid path segment: {segment}.", nameof(path));
            }

            uint childIndex = hardened ? segmentValue + HardenedOffset : segmentValue;
            (BigInteger keyBigInt, chainCode) = DeriveChild(key, chainCode, childIndex, curveOrder);
            key = KeyToBytes(keyBigInt, curveOrder);
        }

        return key;
    }

    private static (byte[] Key, byte[] ChainCode) GetMasterKeyFromSeed(byte[] seed)
    {
        byte[] i = HmacSha512(BitcoinSeedUtf8, seed);
        byte[] key = new byte[KeyLengthBytes];
        byte[] chainCode = new byte[ChainCodeLengthBytes];
        Buffer.BlockCopy(i, 0, key, 0, KeyLengthBytes);
        Buffer.BlockCopy(i, KeyLengthBytes, chainCode, 0, ChainCodeLengthBytes);
        return (key, chainCode);
    }

    private static (BigInteger KeyBigInt, byte[] ChainCode) DeriveChild(byte[] parentKey, byte[] chainCode, uint index, BigInteger curveOrder)
    {
        byte[] data;
        if (index >= HardenedOffset)
        {
            data = new byte[1 + KeyLengthBytes + sizeof(uint)];
            data[0] = 0;
            Buffer.BlockCopy(parentKey, 0, data, 1, KeyLengthBytes);
            data[1 + KeyLengthBytes] = (byte)(index >> 24);
            data[1 + KeyLengthBytes + 1] = (byte)(index >> 16);
            data[1 + KeyLengthBytes + 2] = (byte)(index >> 8);
            data[1 + KeyLengthBytes + 3] = (byte)index;
        }
        else
        {
            byte[] publicKey = Secp256k1Impl.GetPublicKey(parentKey);
            data = new byte[CompressedPublicKeyLengthBytes + sizeof(uint)];
            Buffer.BlockCopy(publicKey, 0, data, 0, CompressedPublicKeyLengthBytes);
            data[CompressedPublicKeyLengthBytes] = (byte)(index >> 24);
            data[CompressedPublicKeyLengthBytes + 1] = (byte)(index >> 16);
            data[CompressedPublicKeyLengthBytes + 2] = (byte)(index >> 8);
            data[CompressedPublicKeyLengthBytes + 3] = (byte)index;
        }

        byte[] i = HmacSha512(chainCode, data);
        BigInteger il = new BigInteger(1, i, 0, KeyLengthBytes);
        byte[] ir = new byte[ChainCodeLengthBytes];
        Buffer.BlockCopy(i, KeyLengthBytes, ir, 0, ChainCodeLengthBytes);

        BigInteger parentKeyBi = new BigInteger(1, parentKey);
        BigInteger newKey = parentKeyBi.Add(il).Mod(curveOrder);
        if (newKey.SignValue == 0)
        {
            throw new InvalidOperationException("BIP32 derivation produced invalid key (zero).");
        }

        return (newKey, ir);
    }

    private static byte[] KeyToBytes(BigInteger key, BigInteger curveOrder)
    {
        byte[] bytes = key.ToByteArrayUnsigned();
        if (bytes.Length > KeyLengthBytes)
        {
            bytes = bytes.AsSpan(bytes.Length - KeyLengthBytes).ToArray();
        }
        else if (bytes.Length < KeyLengthBytes)
        {
            var padded = new byte[KeyLengthBytes];
            Buffer.BlockCopy(bytes, 0, padded, KeyLengthBytes - bytes.Length, bytes.Length);
            bytes = padded;
        }

        return bytes;
    }

    private static byte[] HmacSha512(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA512(key);
        byte[] result = hmac.ComputeHash(data);
        if (result.Length != HmacSha512OutputLengthBytes)
        {
            throw new InvalidOperationException("HMAC-SHA512 must produce 64 bytes.");
        }

        return result;
    }
}
