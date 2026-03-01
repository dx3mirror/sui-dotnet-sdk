namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Wrapper around BCS-serialized bytes with a known schema, supporting conversion to hex/base58/base64 and parsing back.
/// </summary>
/// <typeparam name="T">The type this BCS represents.</typeparam>
public sealed class SerializedBcs<T>
{
    private readonly BcsType<T> _schema;
    private readonly byte[] _bytes;

    /// <summary>
    /// Creates a serialized BCS value (typically from <see cref="BcsType{T}.Serialize"/>).
    /// </summary>
    /// <param name="schema">The BCS type that was used to serialize.</param>
    /// <param name="bytes">The serialized bytes (not modified; stored by reference if possible).</param>
    public SerializedBcs(BcsType<T> schema, byte[] bytes)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }

    /// <summary>
    /// Returns the raw BCS bytes (a copy to prevent mutation).
    /// </summary>
    public byte[] ToBytes()
    {
        return (byte[])_bytes.Clone();
    }

    /// <summary>
    /// Returns the bytes as a lowercase hex string (no prefix).
    /// </summary>
    public string ToHex()
    {
        return MystenLabs.Sui.Utils.Hex.Encode(_bytes);
    }

    /// <summary>
    /// Returns the bytes as a Base64 string.
    /// </summary>
    public string ToBase64()
    {
        return MystenLabs.Sui.Utils.Base64.Encode(_bytes);
    }

    /// <summary>
    /// Returns the bytes as a Base58 string (Bitcoin alphabet).
    /// </summary>
    public string ToBase58()
    {
        return MystenLabs.Sui.Utils.Base58.Encode(_bytes);
    }

    /// <summary>
    /// Deserializes the bytes back to a value of type <typeparamref name="T"/>.
    /// </summary>
    public T Parse()
    {
        return _schema.Parse(_bytes);
    }
}
