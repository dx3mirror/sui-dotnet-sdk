namespace MystenLabs.Sui.Cryptography;

/// <summary>
/// Minimal Bech32 (BIP173) encode/decode for Sui private key encoding (suiprivkey prefix).
/// Not thread-safe for shared mutable state; stateless for single-threaded use.
/// </summary>
internal static class Bech32
{
    private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
    private const int ChecksumLength = 6;
    private const uint PolymodGenerator = 0x3ffffff;
    private const uint PolymodXor = 1u;
    private const int PolymodTopShift = 25;
    private const uint PolymodLowMask = 0x1ffffff;
    private const int PolymodDataShift = 5;
    private const uint PolymodCoeff1 = 0x3b6a57b2;
    private const uint PolymodCoeff2 = 0x26508e6d;
    private const uint PolymodCoeff4 = 0x1ea119fa;
    private const uint PolymodCoeff8 = 0x3d4233dd;
    private const uint PolymodCoeff16 = 0x2a1462c3;
    private const int HrpLowBitsMask = 31;
    private const int Bech32MinLength = 8;
    private const int Bech32MinDataAfterSep = 7;
    private const char Bech32SeparatorCharacter = '1';
    private const uint PolymodTopBit0 = 1;
    private const uint PolymodTopBit1 = 2;
    private const uint PolymodTopBit2 = 4;
    private const uint PolymodTopBit3 = 8;
    private const uint PolymodTopBit4 = 16;
    private const int BitsPerBech32Character = 5;
    private const int BitsPerByte = 8;

    private static uint Polymod(ReadOnlySpan<byte> values)
    {
        uint checksum = 1;
        for (int index = 0; index < values.Length; index++)
        {
            uint top = checksum >> PolymodTopShift;
            checksum = ((checksum & PolymodLowMask) << PolymodDataShift) ^ values[index];
            if ((top & PolymodTopBit0) != 0)
            {
                checksum ^= PolymodCoeff1;
            }

            if ((top & PolymodTopBit1) != 0)
            {
                checksum ^= PolymodCoeff2;
            }

            if ((top & PolymodTopBit2) != 0)
            {
                checksum ^= PolymodCoeff4;
            }

            if ((top & PolymodTopBit3) != 0)
            {
                checksum ^= PolymodCoeff8;
            }

            if ((top & PolymodTopBit4) != 0)
            {
                checksum ^= PolymodCoeff16;
            }
        }

        return checksum;
    }

    private static byte[] HrpExpand(string hrp)
    {
        int length = hrp.Length;
        byte[] result = new byte[length * 2 + 1];
        for (int index = 0; index < length; index++)
        {
            result[index] = (byte)(hrp[index] >> PolymodDataShift);
            result[length + 1 + index] = (byte)(hrp[index] & HrpLowBitsMask);
        }

        return result;
    }

    private static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
    {
        int accumulator = 0;
        int bits = 0;
        var result = new List<byte>();
        int maxValue = (1 << toBits) - 1;
        for (int index = 0; index < data.Length; index++)
        {
            accumulator = (accumulator << fromBits) | data[index];
            bits += fromBits;
            while (bits >= toBits)
            {
                bits -= toBits;
                result.Add((byte)((accumulator >> bits) & maxValue));
            }
        }

        if (pad && bits > 0)
        {
            result.Add((byte)((accumulator << (toBits - bits)) & maxValue));
        }

        return result.ToArray();
    }

    /// <summary>
    /// Encodes HRP and 8-bit data to a Bech32 string (data is converted to 5-bit, checksum appended).
    /// </summary>
    public static string Encode(string hrp, byte[] data)
    {
        if (string.IsNullOrEmpty(hrp))
        {
            throw new ArgumentNullException(nameof(hrp));
        }

        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        byte[] values = ConvertBits(data, BitsPerByte, BitsPerBech32Character, true);
        byte[] hrpExpanded = HrpExpand(hrp);
        var combined = new byte[hrpExpanded.Length + values.Length + ChecksumLength];
        hrpExpanded.CopyTo(combined, 0);
        values.CopyTo(combined, hrpExpanded.Length);
        uint polymodResult = Polymod(combined) ^ PolymodXor;
        for (int index = 0; index < ChecksumLength; index++)
        {
            combined[hrpExpanded.Length + values.Length + index] = (byte)((polymodResult >> (PolymodDataShift * (ChecksumLength - 1 - index))) & HrpLowBitsMask);
        }

        var output = new System.Text.StringBuilder(hrp.Length + 1 + values.Length + ChecksumLength);
        output.Append(hrp.ToLowerInvariant());
        output.Append(Bech32SeparatorCharacter);
        for (int index = 0; index < values.Length + ChecksumLength; index++)
        {
            output.Append(Charset[combined[hrpExpanded.Length + index]]);
        }

        return output.ToString();
    }

    /// <summary>
    /// Decodes a Bech32 string and returns (hrp, 8-bit data). Throws on invalid checksum or format.
    /// </summary>
    public static (string Hrp, byte[] Data) Decode(string bech32)
    {
        if (string.IsNullOrEmpty(bech32))
        {
            throw new ArgumentNullException(nameof(bech32));
        }

        if (bech32.Length < Bech32MinLength)
        {
            throw new ArgumentException("Bech32 string too short.", nameof(bech32));
        }

        bech32 = bech32.ToLowerInvariant();
        int sep = bech32.LastIndexOf(Bech32SeparatorCharacter);
        if (sep < 1 || sep + Bech32MinDataAfterSep > bech32.Length)
        {
            throw new ArgumentException("Invalid Bech32: missing separator or too short data.", nameof(bech32));
        }

        string hrp = bech32[..sep];
        byte[] data = new byte[bech32.Length - sep - 1];
        for (int index = 0; index < data.Length; index++)
        {
            int characterIndex = Charset.IndexOf(bech32[sep + 1 + index]);
            if (characterIndex < 0)
            {
                throw new ArgumentException($"Invalid Bech32 character at position {sep + 1 + index}.", nameof(bech32));
            }

            data[index] = (byte)characterIndex;
        }

        byte[] hrpExpanded = HrpExpand(hrp);
        var combined = new byte[hrpExpanded.Length + data.Length];
        hrpExpanded.CopyTo(combined, 0);
        data.CopyTo(combined, hrpExpanded.Length);
        if (Polymod(combined) != PolymodXor)
        {
            throw new ArgumentException("Invalid Bech32 checksum.", nameof(bech32));
        }

        byte[] decoded = ConvertBits(data.AsSpan(0, data.Length - ChecksumLength).ToArray(), BitsPerBech32Character, BitsPerByte, false);
        return (hrp, decoded);
    }
}
