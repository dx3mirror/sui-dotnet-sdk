namespace MystenLabs.Sui.Keypairs.Secp256k1;

using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

/// <summary>
/// Secp256k1 (K-256) sign and verify using BouncyCastle. Sui uses 32-byte private key, 33-byte compressed public key, 64-byte compact signature (r || s).
/// </summary>
internal static class Secp256k1Impl
{
    private const int PrivateKeySizeBytes = 32;
    private const int CompressedPublicKeySizeBytes = 33;
    private const int SignatureComponentSizeBytes = 32;

    private static readonly X9ECParameters Curve = SecNamedCurves.GetByName("secp256k1");
    private static readonly Org.BouncyCastle.Math.BigInteger CurveOrder = Curve.N;

    /// <summary>
    /// Derives the compressed 33-byte public key from a 32-byte private key.
    /// </summary>
    public static byte[] GetPublicKey(byte[] privateKey)
    {
        if (privateKey == null || privateKey.Length != PrivateKeySizeBytes)
        {
            throw new ArgumentException($"Private key must be {PrivateKeySizeBytes} bytes.", nameof(privateKey));
        }

        var domain = new ECDomainParameters(Curve.Curve, Curve.G, Curve.N, Curve.H);
        var privateKeyParameters = new ECPrivateKeyParameters(new BigInteger(1, privateKey), domain);
        Org.BouncyCastle.Math.EC.ECPoint publicKeyPoint = Curve.G.Multiply(privateKeyParameters.D);
        return publicKeyPoint.GetEncoded(true);
    }

    /// <summary>
    /// Signs the given digest (32 bytes) and returns a 64-byte compact signature (r || s, big-endian). Uses low-S form.
    /// </summary>
    public static byte[] Sign(byte[] privateKey, ReadOnlySpan<byte> digest)
    {
        if (privateKey == null || privateKey.Length != PrivateKeySizeBytes)
        {
            throw new ArgumentException($"Private key must be {PrivateKeySizeBytes} bytes.", nameof(privateKey));
        }

        var domain = new ECDomainParameters(Curve.Curve, Curve.G, Curve.N, Curve.H);
        var privateKeyParameters = new ECPrivateKeyParameters(new BigInteger(1, privateKey), domain);
        var signer = new ECDsaSigner();
        signer.Init(true, privateKeyParameters);
        Org.BouncyCastle.Math.BigInteger[] signatureComponents = signer.GenerateSignature(digest.ToArray());
        Org.BouncyCastle.Math.BigInteger rComponent = signatureComponents[0];
        Org.BouncyCastle.Math.BigInteger sComponent = signatureComponents[1];

        if (sComponent.CompareTo(CurveOrder.ShiftRight(1)) > 0)
        {
            sComponent = CurveOrder.Subtract(sComponent);
        }

        byte[] result = new byte[SignatureComponentSizeBytes * 2];
        CopyBigIntegerToBytes(rComponent, result.AsSpan(0, SignatureComponentSizeBytes));
        CopyBigIntegerToBytes(sComponent, result.AsSpan(SignatureComponentSizeBytes, SignatureComponentSizeBytes));
        return result;
    }

    /// <summary>
    /// Verifies a 64-byte compact signature (r || s) over the given digest using the 33-byte compressed public key.
    /// </summary>
    public static bool Verify(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> digest, byte[] publicKey)
    {
        if (publicKey == null || publicKey.Length != CompressedPublicKeySizeBytes)
        {
            return false;
        }

        if (signature.Length != SignatureComponentSizeBytes * 2)
        {
            return false;
        }

        try
        {
            var domain = new ECDomainParameters(Curve.Curve, Curve.G, Curve.N, Curve.H);
            Org.BouncyCastle.Math.EC.ECPoint publicKeyPoint = Curve.Curve.DecodePoint(publicKey);
            var publicKeyParameters = new ECPublicKeyParameters(publicKeyPoint, domain);
            var signer = new ECDsaSigner();
            signer.Init(false, publicKeyParameters);
            Org.BouncyCastle.Math.BigInteger rComponent = new Org.BouncyCastle.Math.BigInteger(1, signature.Slice(0, SignatureComponentSizeBytes).ToArray());
            Org.BouncyCastle.Math.BigInteger sComponent = new Org.BouncyCastle.Math.BigInteger(1, signature.Slice(SignatureComponentSizeBytes, SignatureComponentSizeBytes).ToArray());
            return signer.VerifySignature(digest.ToArray(), rComponent, sComponent);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the Secp256k1 curve order (for BIP32 derivation). Internal for use by Bip32.
    /// </summary>
    internal static Org.BouncyCastle.Math.BigInteger GetCurveOrder()
    {
        return CurveOrder;
    }

    private static void CopyBigIntegerToBytes(Org.BouncyCastle.Math.BigInteger value, Span<byte> destination)
    {
        byte[] bigIntegerBytes = value.ToByteArrayUnsigned();
        if (bigIntegerBytes.Length > destination.Length)
        {
            bigIntegerBytes.AsSpan(bigIntegerBytes.Length - destination.Length).CopyTo(destination);
        }
        else
        {
            destination.Fill(0);
            bigIntegerBytes.CopyTo(destination.Slice(destination.Length - bigIntegerBytes.Length));
        }
    }
}
