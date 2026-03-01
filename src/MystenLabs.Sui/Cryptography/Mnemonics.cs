namespace MystenLabs.Sui.Cryptography;

using System.Security.Cryptography;
using System.Text;
using MystenLabs.Sui.Utils;

/// <summary>
/// BIP39 mnemonic seed derivation and Sui derivation path validation (SLIP-0010, BIP-32).
/// </summary>
public static class Mnemonics
{
    private const string Bip39MnemonicSaltPrefix = "mnemonic";
    private const int Bip39Pbkdf2IterationCount = 2048;
    private const int Bip39SeedLengthBytes = 64;

    /// <summary>
    /// Validates a SLIP-0010 hardened path: m/44'/784'/{account}'/{change}'/{address}'.
    /// </summary>
    /// <param name="path">Derivation path (e.g. m/44'/784'/0'/0'/0').</param>
    /// <returns>True if the path matches the required form.</returns>
    public static bool IsValidHardenedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return s_hardenedPathRegex.IsMatch(path);
    }

    /// <summary>
    /// Validates a BIP-32 path for Secp256k1 (m/54'/784'/...) or Secp256r1 (m/74'/784'/...) with non-hardened last two segments.
    /// </summary>
    /// <param name="path">Derivation path (e.g. m/54'/784'/0'/0/0).</param>
    /// <returns>True if the path matches.</returns>
    public static bool IsValidBIP32Path(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return s_bip32PathRegex.IsMatch(path);
    }

    /// <summary>
    /// Derives a 64-byte BIP39 seed from a mnemonic phrase with empty passphrase.
    /// </summary>
    /// <param name="mnemonic">Space-separated mnemonic words (e.g. 12 words).</param>
    /// <returns>64-byte seed.</returns>
    public static byte[] MnemonicToSeed(string mnemonic)
    {
        return MnemonicToSeed(mnemonic, string.Empty);
    }

    /// <summary>
    /// Derives a 64-byte BIP39 seed from a mnemonic phrase and optional passphrase.
    /// </summary>
    /// <param name="mnemonic">Space-separated mnemonic words.</param>
    /// <param name="passphrase">Passphrase (empty string for none).</param>
    /// <returns>64-byte seed.</returns>
    public static byte[] MnemonicToSeed(string mnemonic, string passphrase)
    {
        if (string.IsNullOrWhiteSpace(mnemonic))
        {
            throw new ArgumentException("Mnemonic cannot be null or empty.", nameof(mnemonic));
        }

        byte[] password = Encoding.UTF8.GetBytes(mnemonic.Trim());
        byte[] salt = Encoding.UTF8.GetBytes(Bip39MnemonicSaltPrefix + passphrase);
        return Pbkdf2HmacSha512(password, salt, Bip39Pbkdf2IterationCount, Bip39SeedLengthBytes);
    }

    /// <summary>
    /// Derives the BIP39 seed and returns it as a lowercase hex string (for Ed25519 HD derivation).
    /// </summary>
    /// <param name="mnemonic">Space-separated mnemonic words.</param>
    /// <returns>Hex string of the 64-byte seed.</returns>
    public static string MnemonicToSeedHex(string mnemonic)
    {
        byte[] seed = MnemonicToSeed(mnemonic);
        return Hex.Encode(seed);
    }

    private static byte[] Pbkdf2HmacSha512(byte[] password, byte[] salt, int iterationCount, int outputLengthBytes)
    {
        const int blockSizeBytes = 64;
        int blockCount = (outputLengthBytes + blockSizeBytes - 1) / blockSizeBytes;
        var result = new byte[blockCount * blockSizeBytes];

        using var hmac = new HMACSHA512(password);
        var saltWithBlock = new byte[salt.Length + sizeof(uint)];

        Buffer.BlockCopy(salt, 0, saltWithBlock, 0, salt.Length);

        for (int blockIndex = 1; blockIndex <= blockCount; blockIndex++)
        {
            uint bigEndianBlock = (uint)((blockIndex >> 24) & 0xFF) | (uint)((blockIndex >> 16) & 0xFF) << 8 |
                (uint)((blockIndex >> 8) & 0xFF) << 16 | (uint)(blockIndex & 0xFF) << 24;

            saltWithBlock[salt.Length + 0] = (byte)(bigEndianBlock >> 24);
            saltWithBlock[salt.Length + 1] = (byte)(bigEndianBlock >> 16);
            saltWithBlock[salt.Length + 2] = (byte)(bigEndianBlock >> 8);
            saltWithBlock[salt.Length + 3] = (byte)bigEndianBlock;

            byte[] u = hmac.ComputeHash(saltWithBlock);
            Buffer.BlockCopy(u, 0, result, (blockIndex - 1) * blockSizeBytes, blockSizeBytes);

            for (int iteration = 1; iteration < iterationCount; iteration++)
            {
                u = hmac.ComputeHash(u);
                for (int byteIndex = 0; byteIndex < blockSizeBytes; byteIndex++)
                {
                    result[(blockIndex - 1) * blockSizeBytes + byteIndex] ^= u[byteIndex];
                }
            }
        }

        if (outputLengthBytes < result.Length)
        {
            var trimmed = new byte[outputLengthBytes];
            Buffer.BlockCopy(result, 0, trimmed, 0, outputLengthBytes);
            return trimmed;
        }

        return result;
    }

    private static readonly System.Text.RegularExpressions.Regex s_hardenedPathRegex = new(@"^m/44'/784'/[0-9]+'/[0-9]+'/[0-9]+'$");
    private static readonly System.Text.RegularExpressions.Regex s_bip32PathRegex = new(@"^m/(54|74)'/784'/[0-9]+'/[0-9]+/[0-9]+$");
}
