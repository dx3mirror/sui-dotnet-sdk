namespace MystenLabs.Sui.Bcs;

/// <summary>
/// ULEB128 (Unsigned Little Endian Base 128) encoding and decoding for BCS.
/// </summary>
public static class Uleb128
{
    private const byte ContinueMask = 0x80;
    private const byte ValueMask = 0x7F;
    private const int BitsPerByte = 7;

    /// <summary>
    /// Maximum number of bytes for a ULEB128-encoded 64-bit value.
    /// </summary>
    public const int MaxBytesForU64 = 10;

    /// <summary>
    /// Encodes an unsigned 64-bit value in ULEB128 format.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>Encoded bytes (1 to 10 bytes).</returns>
    public static byte[] Encode(ulong value)
    {
        Span<byte> buffer = stackalloc byte[MaxBytesForU64];
        int written = Encode(value, buffer);
        return buffer[..written].ToArray();
    }

    /// <summary>
    /// Encodes an unsigned 64-bit value in ULEB128 format into the given span.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">Span to write encoded bytes to.</param>
    /// <returns>Number of bytes written.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when destination is too small.</exception>
    public static int Encode(ulong value, Span<byte> destination)
    {
        const byte Uleb128EncodedZeroByte = 0;
        const int MinBytesForUleb128Zero = 1;
        if (value == 0)
        {
            if (destination.Length < MinBytesForUleb128Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), "At least 1 byte required.");
            }

            destination[0] = Uleb128EncodedZeroByte;
            return MinBytesForUleb128Zero;
        }

        int written = 0;
        ulong remaining = value;

        do
        {
            if (written >= destination.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), "Destination too small for ULEB128 encoding.");
            }

            byte encodedByte = (byte)(remaining & ValueMask);
            remaining >>= BitsPerByte;
            if (remaining != 0)
            {
                encodedByte |= ContinueMask;
            }

            destination[written++] = encodedByte;
        }
        while (remaining != 0);

        return written;
    }

    /// <summary>
    /// Decodes a ULEB128-encoded value from the given span.
    /// </summary>
    /// <param name="source">Bytes containing the ULEB128 value.</param>
    /// <returns>Decoded value and the number of bytes consumed.</returns>
    /// <exception cref="InvalidOperationException">Thrown on buffer overflow or invalid encoding (e.g. more than 10 bytes for u64).</exception>
    public static (ulong Value, int Length) Decode(ReadOnlySpan<byte> source)
    {
        ulong result = 0;
        int shift = 0;
        int length = 0;

        for (; length < source.Length && length < MaxBytesForU64; length++)
        {
            byte currentByte = source[length];
            result += (ulong)(currentByte & ValueMask) << shift;

            if ((currentByte & ContinueMask) == 0)
            {
                length++;
                return (result, length);
            }

            shift += BitsPerByte;
        }

        if (length >= source.Length)
        {
            throw new InvalidOperationException("ULEB decode error: buffer overflow.");
        }

        throw new InvalidOperationException("ULEB decode error: value exceeds 64 bits.");
    }
}
