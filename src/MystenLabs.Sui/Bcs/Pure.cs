namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Helpers to serialize primitive values to BCS bytes for use in <see cref="CallArgPure"/> (programmable transaction inputs).
/// </summary>
public static class Pure
{
    /// <summary>
    /// Serializes a u8 value to BCS bytes for a pure argument.
    /// </summary>
    public static byte[] SerializeU8(byte value)
    {
        return Bcs.U8().Serialize(value).ToBytes();
    }

    /// <summary>
    /// Serializes a u64 value to BCS bytes for a pure argument.
    /// </summary>
    public static byte[] SerializeU64(ulong value)
    {
        return Bcs.U64().Serialize(value).ToBytes();
    }

    /// <summary>
    /// Serializes a bool value to BCS bytes for a pure argument.
    /// </summary>
    public static byte[] SerializeBool(bool value)
    {
        return Bcs.Bool().Serialize(value).ToBytes();
    }

    /// <summary>
    /// Serializes a string value to BCS bytes for a pure argument.
    /// </summary>
    public static byte[] SerializeString(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return Bcs.String().Serialize(value).ToBytes();
    }

    /// <summary>
    /// Serializes an address (0x + 64 hex) to BCS bytes for a pure argument. Normalizes the address before serialization.
    /// </summary>
    public static byte[] SerializeAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentNullException(nameof(address));
        }

        return SuiBcsTypes.Address.Serialize(address).ToBytes();
    }

    /// <summary>
    /// Builds a <see cref="CallArgPure"/> from u64 value (e.g. for amount arguments).
    /// </summary>
    public static CallArgPure U64(ulong value)
    {
        return new CallArgPure(SerializeU64(value));
    }

    /// <summary>
    /// Builds a <see cref="CallArgPure"/> from an address string (e.g. for recipient).
    /// </summary>
    public static CallArgPure Address(string address)
    {
        return new CallArgPure(SerializeAddress(address));
    }

    /// <summary>
    /// Builds a <see cref="CallArgPure"/> from raw BCS bytes (when type is not a simple primitive).
    /// </summary>
    public static CallArgPure FromBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        return new CallArgPure(bytes);
    }
}
