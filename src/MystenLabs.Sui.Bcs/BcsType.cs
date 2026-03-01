namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Describes a BCS type: how to read from a reader, write to a writer, and optionally compute serialized size and validate input.
/// </summary>
/// <typeparam name="T">The decoded/output type.</typeparam>
public sealed class BcsType<T>
{
    private readonly Func<BcsReader, T> _read;
    private readonly Action<T, BcsWriter> _write;
    private readonly Func<T, int?> _serializedSize;
    private readonly Action<T> _validate;

    /// <summary>
    /// Display name of the type (e.g. "u8", "vector&lt;u8&gt;").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a BCS type with the given read/write logic and optional size/validation.
    /// </summary>
    /// <param name="name">Type name for errors and debugging.</param>
    /// <param name="read">Deserializes one value from the reader.</param>
    /// <param name="write">Serializes one value to the writer.</param>
    /// <param name="serializedSize">Optional. Returns fixed byte length for this value, or null if variable.</param>
    /// <param name="validate">Optional. Throws if the value is invalid.</param>
    public BcsType(
        string name,
        Func<BcsReader, T> read,
        Action<T, BcsWriter> write,
        Func<T, int?>? serializedSize = null,
        Action<T>? validate = null)
    {
        Name = name ?? "?";
        _read = read ?? throw new ArgumentNullException(nameof(read));
        _write = write ?? throw new ArgumentNullException(nameof(write));
        _serializedSize = serializedSize ?? (_ => null);
        _validate = validate ?? (_ => { });
    }

    /// <summary>
    /// Reads one value from the reader.
    /// </summary>
    public T Read(BcsReader reader)
    {
        return _read(reader);
    }

    /// <summary>
    /// Validates the value and writes it to the writer.
    /// </summary>
    public void Write(T value, BcsWriter writer)
    {
        _validate(value);
        _write(value, writer);
    }

    /// <summary>
    /// Returns the serialized byte length for the value if known; otherwise null.
    /// </summary>
    public int? GetSerializedSize(T value)
    {
        return _serializedSize(value);
    }

    /// <summary>
    /// Validates the value (throws on invalid).
    /// </summary>
    public void Validate(T value)
    {
        _validate(value);
    }

    /// <summary>
    /// Serializes the value to BCS bytes and returns a <see cref="SerializedBcs{T}"/> wrapper.
    /// </summary>
    public SerializedBcs<T> Serialize(T value, BcsWriterOptions? options = null)
    {
        _validate(value);
        int? size = _serializedSize(value);
        var writer = new BcsWriter(options ?? new BcsWriterOptions { InitialSize = size ?? BcsWriterOptions.DefaultInitialSize });
        _write(value, writer);
        return new SerializedBcs<T>(this, writer.ToBytes());
    }

    /// <summary>
    /// Deserializes one value from the given BCS bytes.
    /// </summary>
    public T Parse(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        var reader = new BcsReader(bytes);
        return _read(reader);
    }

    /// <summary>
    /// Deserializes from a hex string (optional "0x" prefix).
    /// </summary>
    public T FromHex(string hex)
    {
        byte[] bytes = MystenLabs.Sui.Utils.Hex.Decode(hex.AsSpan());
        return Parse(bytes);
    }

    /// <summary>
    /// Deserializes from a Base58 string.
    /// </summary>
    public T FromBase58(string base58)
    {
        byte[] bytes = MystenLabs.Sui.Utils.Base58.Decode(base58.AsSpan());
        return Parse(bytes);
    }

    /// <summary>
    /// Deserializes from a Base64 string.
    /// </summary>
    public T FromBase64(string base64)
    {
        byte[] bytes = MystenLabs.Sui.Utils.Base64.Decode(base64.AsSpan());
        return Parse(bytes);
    }
}
