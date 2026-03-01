namespace MystenLabs.Sui.Keypairs.Ed25519;

using System.Security.Cryptography;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Utils;

/// <summary>
/// Default Ed25519 derivation path for Sui (BIP44-like: m/44'/784'/0'/0'/0').
/// </summary>
public static class Ed25519Constants
{
    public const string DefaultDerivationPath = "m/44'/784'/0'/0'/0'";
}

/// <summary>
/// Ed25519 keypair for signing Sui transactions and messages.
/// </summary>
public sealed class Ed25519Keypair : Keypair
{
    private readonly byte[] _secretKey;
    private readonly byte[] _publicKey;

    /// <summary>
    /// Creates a new keypair from raw 32-byte secret key (seed). The public key is derived.
    /// </summary>
    public Ed25519Keypair(byte[] secretKey)
    {
        if (secretKey == null || secretKey.Length != KeypairConstants.PrivateKeySize)
        {
            throw new ArgumentException(
                $"Secret key must be {KeypairConstants.PrivateKeySize} bytes.",
                nameof(secretKey));
        }

        _secretKey = (byte[])secretKey.Clone();
        _publicKey = Ed25519Impl.GetPublicKey(secretKey);
    }

    /// <summary>
    /// Creates a keypair from a Sui Bech32 private key string (suiprivkey1...).
    /// </summary>
    public static Ed25519Keypair FromSecretKey(string bech32)
    {
        (SignatureScheme scheme, byte[] secretKey) = SuiPrivateKeyEncoding.Decode(bech32);
        if (scheme != SignatureScheme.Ed25519)
        {
            throw new ArgumentException($"Expected Ed25519 key, got {scheme}.", nameof(bech32));
        }

        return new Ed25519Keypair(secretKey);
    }

    /// <summary>
    /// Creates a keypair from raw 32-byte secret key.
    /// </summary>
    public static Ed25519Keypair FromSecretKey(byte[] secretKey, bool skipValidation = false)
    {
        if (secretKey == null || secretKey.Length != KeypairConstants.PrivateKeySize)
        {
            throw new ArgumentException(
                $"Secret key must be {KeypairConstants.PrivateKeySize} bytes.",
                nameof(secretKey));
        }

        if (!skipValidation)
        {
            byte[] testMessage = System.Text.Encoding.UTF8.GetBytes("sui validation");
            byte[] signature = Ed25519Impl.Sign(secretKey, testMessage);
            byte[] publicKey = Ed25519Impl.GetPublicKey(secretKey);
            if (!Ed25519Impl.Verify(signature, testMessage, publicKey))
            {
                throw new ArgumentException("Invalid secret key: verification failed.", nameof(secretKey));
            }
        }

        return new Ed25519Keypair(secretKey);
    }

    /// <summary>
    /// Generates a new random Ed25519 keypair.
    /// </summary>
    public static Ed25519Keypair Generate()
    {
        byte[] secretKey = new byte[KeypairConstants.PrivateKeySize];
        RandomNumberGenerator.Fill(secretKey);
        return new Ed25519Keypair(secretKey);
    }

    /// <summary>
    /// Derives an Ed25519 keypair from a mnemonic phrase and optional derivation path.
    /// Path must be SLIP-0010 form m/44'/784'/{account}'/{change}'/{address}'.
    /// </summary>
    /// <param name="mnemonic">Space-separated mnemonic words (e.g. 12 words).</param>
    /// <param name="path">Optional path; defaults to <see cref="Ed25519Constants.DefaultDerivationPath"/>.</param>
    /// <returns>Derived keypair.</returns>
    public static Ed25519Keypair DeriveKeypair(string mnemonic, string? path = null)
    {
        string derivationPath = path ?? Ed25519Constants.DefaultDerivationPath;
        if (!Mnemonics.IsValidHardenedPath(derivationPath))
        {
            throw new ArgumentException("Invalid derivation path.", nameof(path));
        }

        string seedHex = Mnemonics.MnemonicToSeedHex(mnemonic);
        Ed25519HdKey.DerivedKeys derived = Ed25519HdKey.DerivePath(derivationPath, seedHex);
        return FromSecretKey(derived.Key);
    }

    /// <summary>
    /// Derives an Ed25519 keypair from a 64-byte seed given as hex string and optional derivation path.
    /// Path must be SLIP-0010 form m/44'/784'/{account}'/{change}'/{address}'.
    /// </summary>
    /// <param name="seedHex">64-byte seed as hex string (128 hex chars, optional 0x prefix).</param>
    /// <param name="path">Optional path; defaults to <see cref="Ed25519Constants.DefaultDerivationPath"/>.</param>
    /// <returns>Derived keypair.</returns>
    public static Ed25519Keypair DeriveKeypairFromSeed(string seedHex, string? path = null)
    {
        string derivationPath = path ?? Ed25519Constants.DefaultDerivationPath;
        if (!Mnemonics.IsValidHardenedPath(derivationPath))
        {
            throw new ArgumentException("Invalid derivation path.", nameof(path));
        }

        ReadOnlySpan<char> hexSpan = seedHex.AsSpan().Trim();
        if (hexSpan.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hexSpan = hexSpan[2..];
        }

        Ed25519HdKey.DerivedKeys derived = Ed25519HdKey.DerivePath(derivationPath, hexSpan.ToString());
        return FromSecretKey(derived.Key);
    }

    /// <summary>
    /// Derives an Ed25519 keypair from a 64-byte raw seed and optional derivation path.
    /// </summary>
    /// <param name="seed">64-byte seed (e.g. from <see cref="Mnemonics.MnemonicToSeed(string, string)"/> with empty passphrase).</param>
    /// <param name="path">Optional path; defaults to <see cref="Ed25519Constants.DefaultDerivationPath"/>.</param>
    /// <returns>Derived keypair.</returns>
    public static Ed25519Keypair DeriveKeypairFromSeed(byte[] seed, string? path = null)
    {
        string derivationPath = path ?? Ed25519Constants.DefaultDerivationPath;
        if (!Mnemonics.IsValidHardenedPath(derivationPath))
        {
            throw new ArgumentException("Invalid derivation path.", nameof(path));
        }

        string seedHex = Hex.Encode(seed);
        Ed25519HdKey.DerivedKeys derived = Ed25519HdKey.DerivePath(derivationPath, seedHex);
        return FromSecretKey(derived.Key);
    }

    /// <inheritdoc />
    public override byte[] Sign(ReadOnlySpan<byte> digest)
    {
        return Ed25519Impl.Sign(_secretKey, digest);
    }

    /// <inheritdoc />
    public override SignatureScheme GetKeyScheme()
    {
        return SignatureScheme.Ed25519;
    }

    /// <inheritdoc />
    public override PublicKey GetPublicKey()
    {
        return new Ed25519PublicKey(_publicKey);
    }

    /// <inheritdoc />
    public override string GetSecretKey()
    {
        return SuiPrivateKeyEncoding.Encode(_secretKey, SignatureScheme.Ed25519);
    }
}
