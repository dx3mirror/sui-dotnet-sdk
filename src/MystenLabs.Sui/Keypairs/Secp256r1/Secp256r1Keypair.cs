namespace MystenLabs.Sui.Keypairs.Secp256r1;

using System.Security.Cryptography;
using MystenLabs.Sui.Cryptography;

/// <summary>
/// Default BIP-32 derivation path for Secp256r1 (Sui).
/// </summary>
public static class Secp256r1Constants
{
    public const string DefaultDerivationPath = "m/74'/784'/0'/0/0";
}

/// <summary>
/// Secp256r1 (P-256) keypair for signing Sui transactions and messages.
/// </summary>
public sealed class Secp256r1Keypair : Keypair
{
    private const int PrivateKeySizeBytes = 32;

    private readonly byte[] _secretKey;
    private readonly byte[] _publicKey;

    /// <summary>
    /// Creates a new keypair from raw 32-byte secret key. The public key is derived.
    /// </summary>
    public Secp256r1Keypair(byte[] secretKey)
    {
        if (secretKey == null || secretKey.Length != PrivateKeySizeBytes)
        {
            throw new ArgumentException(
                $"Secret key must be {PrivateKeySizeBytes} bytes.",
                nameof(secretKey));
        }

        _secretKey = (byte[])secretKey.Clone();
        _publicKey = Secp256r1Impl.GetPublicKey(_secretKey);
    }

    /// <summary>
    /// Creates a keypair from a Sui Bech32 private key string (suiprivkey1...).
    /// </summary>
    public static Secp256r1Keypair FromSecretKey(string bech32)
    {
        (SignatureScheme scheme, byte[] secretKey) = SuiPrivateKeyEncoding.Decode(bech32);
        if (scheme != SignatureScheme.Secp256r1)
        {
            throw new ArgumentException($"Expected Secp256r1 key, got {scheme}.", nameof(bech32));
        }

        return new Secp256r1Keypair(secretKey);
    }

    /// <summary>
    /// Creates a keypair from raw 32-byte secret key, with optional validation.
    /// </summary>
    public static Secp256r1Keypair FromSecretKey(byte[] secretKey, bool skipValidation = false)
    {
        if (secretKey == null || secretKey.Length != PrivateKeySizeBytes)
        {
            throw new ArgumentException(
                $"Secret key must be {PrivateKeySizeBytes} bytes.",
                nameof(secretKey));
        }

        if (!skipValidation)
        {
            byte[] testMessage = System.Text.Encoding.UTF8.GetBytes("sui validation");
            byte[] digest = Blake2b.Hash256(testMessage);
            byte[] signature = Secp256r1Impl.Sign(secretKey, digest);
            byte[] publicKey = Secp256r1Impl.GetPublicKey(secretKey);
            if (!Secp256r1Impl.Verify(signature, digest, publicKey))
            {
                throw new ArgumentException("Invalid secret key: verification failed.", nameof(secretKey));
            }
        }

        return new Secp256r1Keypair(secretKey);
    }

    /// <summary>
    /// Generates a new random Secp256r1 keypair.
    /// </summary>
    public static Secp256r1Keypair Generate()
    {
        byte[] secretKey = new byte[PrivateKeySizeBytes];
        RandomNumberGenerator.Fill(secretKey);
        return new Secp256r1Keypair(secretKey);
    }

    /// <summary>
    /// Derives a Secp256r1 keypair from a mnemonic phrase and optional BIP-32 path.
    /// Path must be m/74'/784'/{account}'/{change}/{address}. Uses same BIP-32 derivation as Secp256k1; only the 32-byte private key is used for P-256.
    /// </summary>
    /// <param name="mnemonic">Space-separated mnemonic words (e.g. 12 words).</param>
    /// <param name="path">Optional path; defaults to <see cref="Secp256r1Constants.DefaultDerivationPath"/>.</param>
    /// <returns>Derived keypair.</returns>
    public static Secp256r1Keypair DeriveKeypair(string mnemonic, string? path = null)
    {
        string derivationPath = path ?? Secp256r1Constants.DefaultDerivationPath;
        if (!Mnemonics.IsValidBIP32Path(derivationPath))
        {
            throw new ArgumentException("Invalid derivation path.", nameof(path));
        }

        byte[] seed = Mnemonics.MnemonicToSeed(mnemonic);
        byte[] privateKey = Bip32.DerivePrivateKey(seed, derivationPath);
        return new Secp256r1Keypair(privateKey);
    }

    /// <inheritdoc />
    public override byte[] Sign(ReadOnlySpan<byte> digest)
    {
        return Secp256r1Impl.Sign(_secretKey, digest);
    }

    /// <inheritdoc />
    public override SignatureScheme GetKeyScheme()
    {
        return SignatureScheme.Secp256r1;
    }

    /// <inheritdoc />
    public override PublicKey GetPublicKey()
    {
        return new Secp256r1PublicKey(_publicKey);
    }

    /// <inheritdoc />
    public override string GetSecretKey()
    {
        return SuiPrivateKeyEncoding.Encode(_secretKey, SignatureScheme.Secp256r1);
    }
}
