namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Reads BCS-encoded data sequentially. Used by BCS type deserialization.
/// </summary>
public sealed class BcsReader
{
    private const int U16ByteCount = 2;
    private const int U32ByteCount = 4;
    private const int U64ByteCount = 8;
    private const int U128ByteCount = 16;
    private const int U256ByteCount = 32;

    private readonly byte[] _data;
    private int _position;

    /// <summary>
    /// Creates a reader over the given byte array. The array is not copied; do not modify it while reading.
    /// Prefer this overload when you already have a byte[] to avoid the copy made by the <see cref="BcsReader(ReadOnlySpan{byte})"/> overload.
    /// </summary>
    /// <param name="data">BCS-serialized bytes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    public BcsReader(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _position = 0;
    }

    /// <summary>
    /// Creates a reader over a copy of the given span (so the reader owns the buffer).
    /// </summary>
    /// <param name="data">BCS-serialized bytes.</param>
    public BcsReader(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
        _position = 0;
    }

    /// <summary>
    /// Current read position (number of bytes consumed).
    /// </summary>
    public int Position => _position;

    /// <summary>
    /// Number of bytes available to read.
    /// </summary>
    public int Remaining => _data.Length - _position;

    /// <summary>
    /// Advances the position by <paramref name="bytes"/>.
    /// </summary>
    /// <param name="bytes">Number of bytes to skip.</param>
    /// <returns>This reader for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when advancing past the end of the buffer.</exception>
    public BcsReader Shift(int bytes)
    {
        if (bytes < 0 || _position + bytes > _data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "Cannot shift past end of buffer.");
        }

        _position += bytes;
        return this;
    }

    private void EnsureRemaining(int count)
    {
        if (_position + count > _data.Length)
        {
            throw new InvalidOperationException(
                $"BCS read overflow: need {count} bytes, {Remaining} remaining.");
        }
    }

    /// <summary>
    /// Reads one byte (u8).
    /// </summary>
    public byte Read8()
    {
        EnsureRemaining(1);
        return _data[_position++];
    }

    /// <summary>
    /// Reads a 16-bit unsigned integer (little-endian).
    /// </summary>
    public ushort Read16()
    {
        EnsureRemaining(U16ByteCount);
        ushort value = (ushort)(_data[_position] | (_data[_position + 1] << 8));
        _position += U16ByteCount;
        return value;
    }

    /// <summary>
    /// Reads a 32-bit unsigned integer (little-endian).
    /// </summary>
    public uint Read32()
    {
        EnsureRemaining(U32ByteCount);
        uint value = (uint)(_data[_position] | (_data[_position + 1] << 8) | (_data[_position + 2] << 16) | (_data[_position + 3] << 24));
        _position += U32ByteCount;
        return value;
    }

    /// <summary>
    /// Reads a 64-bit unsigned integer (little-endian).
    /// </summary>
    public ulong Read64()
    {
        ulong low = Read32();
        ulong high = Read32();
        return low | (high << 32);
    }

    /// <summary>
    /// Reads a 128-bit unsigned integer (little-endian), returned as decimal string for compatibility with TS.
    /// </summary>
    public string Read128()
    {
        EnsureRemaining(U128ByteCount);
        byte[] bytes = new byte[U128ByteCount + 1];
        for (int index = 0; index < U128ByteCount; index++)
        {
            bytes[index] = _data[_position + index];
        }

        _position += U128ByteCount;
        var big = new System.Numerics.BigInteger(bytes);
        return big.ToString();
    }

    /// <summary>
    /// Reads a 256-bit unsigned integer (little-endian), returned as decimal string for compatibility with TS.
    /// </summary>
    public string Read256()
    {
        EnsureRemaining(U256ByteCount);
        byte[] bytes = new byte[U256ByteCount + 1];
        for (int index = 0; index < U256ByteCount; index++)
        {
            bytes[index] = _data[_position + index];
        }

        _position += U256ByteCount;
        var big = new System.Numerics.BigInteger(bytes);
        return big.ToString();
    }

    /// <summary>
    /// Reads exactly <paramref name="byteCount"/> bytes.
    /// </summary>
    /// <param name="byteCount">Number of bytes to read.</param>
    /// <returns>Read bytes.</returns>
    public byte[] ReadBytes(int byteCount)
    {
        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount), "Byte count must be non-negative.");
        }

        EnsureRemaining(byteCount);
        byte[] result = new byte[byteCount];
        Array.Copy(_data, _position, result, 0, byteCount);
        _position += byteCount;
        return result;
    }

    /// <summary>
    /// Reads a ULEB128-encoded unsigned integer.
    /// </summary>
    public ulong ReadUleb128()
    {
        (ulong value, int length) = Uleb128.Decode(_data.AsSpan(_position));
        _position += length;
        return value;
    }

    /// <summary>
    /// Reads a BCS vector: ULEB length then <paramref name="readElement"/> invoked length times.
    /// </summary>
    /// <param name="readElement">Callback (reader, index, length) returning one element.</param>
    /// <returns>Array of decoded elements.</returns>
    public T[] ReadVec<T>(Func<BcsReader, int, int, T> readElement)
    {
        if (readElement == null)
        {
            throw new ArgumentNullException(nameof(readElement));
        }

        int length = (int)ReadUleb128();
        T[] result = new T[length];
        for (int index = 0; index < length; index++)
        {
            result[index] = readElement(this, index, length);
        }

        return result;
    }
}
