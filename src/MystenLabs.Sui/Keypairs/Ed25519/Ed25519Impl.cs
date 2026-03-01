namespace MystenLabs.Sui.Keypairs.Ed25519;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

/// <summary>
/// Ed25519 sign/verify and key derivation using BouncyCastle (32-byte seed as private key).
/// </summary>
internal static class Ed25519Impl
{
    public static byte[] GetPublicKey(byte[] secretKey)
    {
        var privateKey = new Ed25519PrivateKeyParameters(secretKey, 0);
        var publicKey = privateKey.GeneratePublicKey();
        return publicKey.GetEncoded();
    }

    public static byte[] Sign(byte[] secretKey, ReadOnlySpan<byte> data)
    {
        var privateKey = new Ed25519PrivateKeyParameters(secretKey, 0);
        var signer = new Ed25519Signer();
        signer.Init(true, privateKey);
        signer.BlockUpdate(data.ToArray(), 0, data.Length);
        return signer.GenerateSignature();
    }

    public static bool Verify(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> data, byte[] publicKey)
    {
        var publicKeyParameters = new Ed25519PublicKeyParameters(publicKey, 0);
        var signer = new Ed25519Signer();
        signer.Init(false, publicKeyParameters);
        signer.BlockUpdate(data.ToArray(), 0, data.Length);
        return signer.VerifySignature(signature.ToArray());
    }
}
