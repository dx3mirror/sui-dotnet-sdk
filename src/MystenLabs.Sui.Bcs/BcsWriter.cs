namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Writes primitive and length-prefixed values in BCS (Binary Canonical Serialization) format for Sui.
/// Supports configurable buffer growth and chainable write methods.
/// </summary>
public sealed class BcsWriter
{
    private const int U16ByteCount = 2;
    private const int U32ByteCount = 4;
    private const int U64ByteCount = 8;
    private const int U128ByteCount = 16;
    private const int U256ByteCount = 32;
    private const byte Uleb128ContinueMask = 0x80;
    private const byte Uleb128ValueMask = 0x7F;
    private const int Uleb128BitsPerByte = 7;

    private readonly List<byte> _buffer;
    private readonly int _maxSize;
    private readonly int _allocateSize;

    /// <summary>
    /// Creates a writer with default options (1 KB initial size, no max size limit).
    /// </summary>
    public BcsWriter()
        : this(new BcsWriterOptions())
    {
    }

    /// <summary>
    /// Creates a writer with the given options.
    /// </summary>
    /// <param name="options">Buffer size and growth options; if null, defaults are used.</param>
    public BcsWriter(BcsWriterOptions? options)
    {
        options ??= new BcsWriterOptions();
        int initial = options.InitialSize > 0 ? options.InitialSize : BcsWriterOptions.DefaultInitialSize;
        _maxSize = options.MaxSize > 0 ? options.MaxSize : int.MaxValue;
        _allocateSize = options.AllocateSize > 0 ? options.AllocateSize : BcsWriterOptions.DefaultAllocateSize;
        _buffer = new List<byte>(Math.Min(initial, _maxSize));
    }

    /// <summary>
    /// Ensures the buffer has room for at least <paramref name="bytes"/> more bytes; grows up to max size.
    /// </summary>
    private void EnsureCapacity(int bytes)
    {
        int required = _buffer.Count + bytes;
        if (required > _maxSize)
        {
            throw new InvalidOperationException(
                $"BCS buffer would exceed max size. Current: {_buffer.Count}, Max: {_maxSize}, Required: {required}.");
        }

        if (required > _buffer.Capacity)
        {
            int allocate = Math.Max(_allocateSize, bytes);
            _buffer.Capacity = Math.Min(_maxSize, Math.Max(_buffer.Capacity + allocate, required));
        }
    }

    /// <summary>
    /// Writes a single byte (unsigned 8-bit integer). Chainable.
    /// </summary>
    /// <param name="value">The byte value to write.</param>
    /// <returns>This writer for chaining.</returns>
    public BcsWriter WriteU8(byte value)
    {
        EnsureCapacity(1);
        _buffer.Add(value);
        return this;
    }

    /// <summary>
    /// Writes a 16-bit unsigned integer in little-endian order. Chainable.
    /// </summary>
    public BcsWriter WriteU16(ushort value)
    {
        EnsureCapacity(U16ByteCount);
        for (int index = 0; index < U16ByteCount; index++)
        {
            _buffer.Add((byte)(value >> (index * 8)));
        }

        return this;
    }

    /// <summary>
    /// Writes a 32-bit unsigned integer in little-endian order. Chainable.
    /// </summary>
    public BcsWriter WriteU32(uint value)
    {
        EnsureCapacity(U32ByteCount);
        for (int index = 0; index < U32ByteCount; index++)
        {
            _buffer.Add((byte)(value >> (index * 8)));
        }

        return this;
    }

    /// <summary>
    /// Writes a 64-bit unsigned integer in little-endian order. Chainable.
    /// </summary>
    public BcsWriter WriteU64(ulong value)
    {
        EnsureCapacity(U64ByteCount);
        for (int index = 0; index < U64ByteCount; index++)
        {
            _buffer.Add((byte)(value >> (index * 8)));
        }

        return this;
    }

    /// <summary>
    /// Writes a 128-bit unsigned integer in little-endian order. Chainable.
    /// </summary>
    public BcsWriter WriteU128(ulong low, ulong high)
    {
        EnsureCapacity(U128ByteCount);
        WriteU64(low);
        WriteU64(high);
        return this;
    }

    /// <summary>
    /// Writes a 128-bit unsigned integer from a big-integer value (0 to 2^128-1). Chainable.
    /// </summary>
    public BcsWriter WriteU128(System.Numerics.BigInteger value)
    {
        EnsureCapacity(U128ByteCount);
        byte[] bytes = value.ToByteArray();
        for (int index = 0; index < U128ByteCount; index++)
        {
            _buffer.Add(index < bytes.Length ? bytes[index] : (byte)0);
        }

        return this;
    }

    /// <summary>
    /// Writes a 256-bit unsigned integer from a big-integer value (0 to 2^256-1). Chainable.
    /// </summary>
    public BcsWriter WriteU256(System.Numerics.BigInteger value)
    {
        EnsureCapacity(U256ByteCount);
        byte[] bytes = value.ToByteArray();
        for (int index = 0; index < U256ByteCount; index++)
        {
            _buffer.Add(index < bytes.Length ? bytes[index] : (byte)0);
        }

        return this;
    }

    /// <summary>
    /// Writes raw bytes. Chainable.
    /// </summary>
    public BcsWriter WriteBytes(ReadOnlySpan<byte> bytes)
    {
        EnsureCapacity(bytes.Length);
        for (int index = 0; index < bytes.Length; index++)
        {
            _buffer.Add(bytes[index]);
        }

        return this;
    }

    /// <summary>
    /// Writes a length-prefixed byte sequence (length as ULEB128, then raw bytes). Chainable.
    /// </summary>
    public BcsWriter WriteLengthPrefixed(ReadOnlySpan<byte> bytes)
    {
        WriteUleb128((ulong)bytes.Length);
        WriteBytes(bytes);
        return this;
    }

    /// <summary>
    /// Writes an unsigned integer in ULEB128 variable-length encoding. Chainable.
    /// </summary>
    public BcsWriter WriteUleb128(ulong value)
    {
        if (value == 0)
        {
            EnsureCapacity(1);
            _buffer.Add(0);
            return this;
        }

        ulong remaining = value;
        do
        {
            EnsureCapacity(1);
            byte byteValue = (byte)(remaining & Uleb128ValueMask);
            remaining >>= Uleb128BitsPerByte;
            if (remaining != 0)
            {
                byteValue |= Uleb128ContinueMask;
            }

            _buffer.Add(byteValue);
        }
        while (remaining != 0);

        return this;
    }

    /// <summary>
    /// Writes a vector by writing the length (ULEB128) then invoking the callback for each element. Chainable.
    /// </summary>
    /// <param name="vector">Sequence of elements to write.</param>
    /// <param name="writeElement">Called for each element with (this writer, element, index, totalCount).</param>
    public BcsWriter WriteVec<T>(IReadOnlyList<T> vector, Action<BcsWriter, T, int, int> writeElement)
    {
        if (vector == null)
        {
            throw new ArgumentNullException(nameof(vector));
        }

        if (writeElement == null)
        {
            throw new ArgumentNullException(nameof(writeElement));
        }

        WriteUleb128((ulong)vector.Count);
        for (int index = 0; index < vector.Count; index++)
        {
            writeElement(this, vector[index], index, vector.Count);
        }

        return this;
    }

    /// <summary>
    /// Returns the serialized BCS bytes (only the written range).
    /// </summary>
    public byte[] ToBytes()
    {
        return _buffer.ToArray();
    }
}
