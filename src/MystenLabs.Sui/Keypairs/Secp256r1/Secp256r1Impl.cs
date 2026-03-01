namespace MystenLabs.Sui.Keypairs.Secp256r1;

using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

/// <summary>
/// Secp256r1 (P-256 / NIST) sign and verify using .NET ECDsa. Sui uses 32-byte private key, 33-byte compressed public key, 64-byte compact signature (r || s).
/// </summary>
internal static class Secp256r1Impl
{
    private const int PrivateKeySizeBytes = 32;
    private const int CompressedPublicKeySizeBytes = 33;
    private const int SignatureComponentSizeBytes = 32;
    private const int EcCoordinateSizeBytes = 32;

    /// <summary>
    /// P-256 curve order n (for low-S normalization).
    /// </summary>
    private static readonly BigInteger CurveOrderN = new(
        "115792089210356248762697446949407573529996955224135760342422259061068512044369",
        10);

    private const byte DerTagSequence = 0x30;
    private const byte DerTagInteger = 0x02;
    private const byte DerHighBitMask = 0x80;
    private const byte DerPaddingByte = 0x00;
    private const byte CompressedKeyPrefixEvenY = 0x02;
    private const byte CompressedKeyPrefixOddY = 0x03;
    private const int DerSequenceTagAndLengthBytes = 2;
    private const int DerIntegerTagAndLengthBytes = 2;

    /// <summary>
    /// Derives the compressed 33-byte public key from a 32-byte private key.
    /// </summary>
    public static byte[] GetPublicKey(byte[] privateKey)
    {
        if (privateKey == null || privateKey.Length != PrivateKeySizeBytes)
        {
            throw new ArgumentException($"Private key must be {PrivateKeySizeBytes} bytes.", nameof(privateKey));
        }

        using var ecdsa = CreateFromPrivateKey(privateKey);
        return GetCompressedPublicKey(ecdsa);
    }

    /// <summary>
    /// Exports the compressed 33-byte public key (02/03 + X) from an ECDsa instance.
    /// </summary>
    public static byte[] GetCompressedPublicKey(ECDsa ecdsa)
    {
        ECParameters parameters = ecdsa.ExportParameters(false);
        byte[]? affineX = parameters.Q.X;
        byte[]? affineY = parameters.Q.Y;
        if (affineX == null || affineY == null || affineX.Length != EcCoordinateSizeBytes || affineY.Length != EcCoordinateSizeBytes)
        {
            throw new InvalidOperationException($"P-256 public key must have {EcCoordinateSizeBytes}-byte X and Y.");
        }

        byte prefix = (affineY[affineY.Length - 1] & 1) == 0 ? CompressedKeyPrefixEvenY : CompressedKeyPrefixOddY;
        var result = new byte[CompressedPublicKeySizeBytes];
        result[0] = prefix;
        Buffer.BlockCopy(affineX, 0, result, 1, EcCoordinateSizeBytes);
        return result;
    }

    /// <summary>
    /// Signs the given digest (32 bytes) and returns a 64-byte compact signature (r || s). Uses low-S form.
    /// </summary>
    public static byte[] Sign(byte[] privateKey, ReadOnlySpan<byte> digest)
    {
        if (privateKey == null || privateKey.Length != PrivateKeySizeBytes)
        {
            throw new ArgumentException($"Private key must be {PrivateKeySizeBytes} bytes.", nameof(privateKey));
        }

        using var ecdsa = CreateFromPrivateKey(privateKey);
        byte[] derSignature = ecdsa.SignHash(digest.ToArray(), DSASignatureFormat.Rfc3279DerSequence);
        (byte[] rBytes, byte[] sBytes) = ParseDerSignature(derSignature);
        BigInteger sBigInt = new(1, sBytes);
        if (sBigInt.CompareTo(CurveOrderN.ShiftRight(1)) > 0)
        {
            sBigInt = CurveOrderN.Subtract(sBigInt);
            sBytes = sBigInt.ToByteArrayUnsigned();
            if (sBytes.Length > SignatureComponentSizeBytes)
            {
                sBytes = sBytes.AsSpan(sBytes.Length - SignatureComponentSizeBytes).ToArray();
            }
            else if (sBytes.Length < SignatureComponentSizeBytes)
            {
                var padded = new byte[SignatureComponentSizeBytes];
                Buffer.BlockCopy(sBytes, 0, padded, SignatureComponentSizeBytes - sBytes.Length, sBytes.Length);
                sBytes = padded;
            }
        }

        var compact = new byte[SignatureComponentSizeBytes * 2];
        CopyBigIntToFixedBytes(rBytes, compact.AsSpan(0, SignatureComponentSizeBytes));
        CopyBigIntToFixedBytes(sBytes, compact.AsSpan(SignatureComponentSizeBytes, SignatureComponentSizeBytes));
        return compact;
    }

    /// <summary>
    /// Verifies a 64-byte compact signature over the given digest using the 33-byte compressed public key.
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
            using var ecdsa = CreateFromCompressedPublicKey(publicKey);
            byte[] derSignature = ConvertCompactToDerSignature(signature);
            return ecdsa.VerifyHash(digest.ToArray(), derSignature, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch
        {
            return false;
        }
    }

    private static ECDsa CreateFromPrivateKey(byte[] privateKey)
    {
        var parameters = new ECParameters
        {
            Curve = System.Security.Cryptography.ECCurve.NamedCurves.nistP256,
            D = privateKey
        };
        var ecdsa = ECDsa.Create();
        ecdsa.ImportParameters(parameters);
        return ecdsa;
    }

    private static ECDsa CreateFromCompressedPublicKey(byte[] compressed)
    {
        X9ECParameters bcCurve = ECNamedCurveTable.GetByName("P-256");
        Org.BouncyCastle.Math.EC.ECPoint point = bcCurve.Curve.DecodePoint(compressed);
        byte[] affineX = point.AffineXCoord.ToBigInteger().ToByteArrayUnsigned();
        byte[] affineY = point.AffineYCoord.ToBigInteger().ToByteArrayUnsigned();
        if (affineX.Length > EcCoordinateSizeBytes)
        {
            affineX = affineX.AsSpan(affineX.Length - EcCoordinateSizeBytes).ToArray();
        }
        else if (affineX.Length < EcCoordinateSizeBytes)
        {
            var paddedX = new byte[EcCoordinateSizeBytes];
            Buffer.BlockCopy(affineX, 0, paddedX, EcCoordinateSizeBytes - affineX.Length, affineX.Length);
            affineX = paddedX;
        }

        if (affineY.Length > EcCoordinateSizeBytes)
        {
            affineY = affineY.AsSpan(affineY.Length - EcCoordinateSizeBytes).ToArray();
        }
        else if (affineY.Length < EcCoordinateSizeBytes)
        {
            var paddedY = new byte[EcCoordinateSizeBytes];
            Buffer.BlockCopy(affineY, 0, paddedY, EcCoordinateSizeBytes - affineY.Length, affineY.Length);
            affineY = paddedY;
        }

        var parameters = new ECParameters
        {
            Curve = System.Security.Cryptography.ECCurve.NamedCurves.nistP256,
            Q = new System.Security.Cryptography.ECPoint
            {
                X = affineX,
                Y = affineY
            }
        };
        var ecdsa = ECDsa.Create();
        ecdsa.ImportParameters(parameters);
        return ecdsa;
    }

    private static byte[] ConvertDerSignatureToCompact(byte[] derSignature)
    {
        (byte[] rBytes, byte[] sBytes) = ParseDerSignature(derSignature);
        var result = new byte[SignatureComponentSizeBytes * 2];
        CopyBigIntToFixedBytes(rBytes, result.AsSpan(0, SignatureComponentSizeBytes));
        CopyBigIntToFixedBytes(sBytes, result.AsSpan(SignatureComponentSizeBytes, SignatureComponentSizeBytes));
        return result;
    }

    private static byte[] ConvertCompactToDerSignature(ReadOnlySpan<byte> compact)
    {
        byte[] rBytes = compact.Slice(0, SignatureComponentSizeBytes).ToArray();
        byte[] sBytes = compact.Slice(SignatureComponentSizeBytes, SignatureComponentSizeBytes).ToArray();
        return BuildDerSignature(rBytes, sBytes);
    }

    private static (byte[] R, byte[] S) ParseDerSignature(byte[] derEncodedSignature)
    {
        int offset = 0;
        if (derEncodedSignature[offset++] != DerTagSequence)
        {
            throw new InvalidOperationException("Invalid DER signature.");
        }

        offset++;
        if (derEncodedSignature[offset++] != DerTagInteger)
        {
            throw new InvalidOperationException("Invalid DER: expected INTEGER for r.");
        }

        int rLength = derEncodedSignature[offset++];
        byte[] rBytes = new byte[rLength];
        Buffer.BlockCopy(derEncodedSignature, offset, rBytes, 0, rLength);
        offset += rLength;
        if (derEncodedSignature[offset++] != DerTagInteger)
        {
            throw new InvalidOperationException("Invalid DER: expected INTEGER for s.");
        }

        int sLength = derEncodedSignature[offset++];
        byte[] sBytes = new byte[sLength];
        Buffer.BlockCopy(derEncodedSignature, offset, sBytes, 0, sLength);
        return (rBytes, sBytes);
    }

    private static byte[] BuildDerSignature(byte[] rBytes, byte[] sBytes)
    {
        if (rBytes.Length > 0 && (rBytes[0] & DerHighBitMask) != 0)
        {
            rBytes = [DerPaddingByte, ..rBytes];
        }

        if (sBytes.Length > 0 && (sBytes[0] & DerHighBitMask) != 0)
        {
            sBytes = [DerPaddingByte, ..sBytes];
        }

        int totalLength = DerIntegerTagAndLengthBytes + rBytes.Length + DerIntegerTagAndLengthBytes + sBytes.Length;
        var derEncoded = new byte[DerSequenceTagAndLengthBytes + totalLength];
        int position = 0;
        derEncoded[position++] = DerTagSequence;
        derEncoded[position++] = (byte)totalLength;
        derEncoded[position++] = DerTagInteger;
        derEncoded[position++] = (byte)rBytes.Length;
        Buffer.BlockCopy(rBytes, 0, derEncoded, position, rBytes.Length);
        position += rBytes.Length;
        derEncoded[position++] = DerTagInteger;
        derEncoded[position++] = (byte)sBytes.Length;
        Buffer.BlockCopy(sBytes, 0, derEncoded, position, sBytes.Length);
        return derEncoded;
    }

    private static void CopyBigIntToFixedBytes(byte[] value, Span<byte> destination)
    {
        if (value.Length >= destination.Length)
        {
            int skip = value.Length - destination.Length;
            value.AsSpan(skip).CopyTo(destination);
        }
        else
        {
            destination.Fill(0);
            value.CopyTo(destination.Slice(destination.Length - value.Length));
        }
    }
}
