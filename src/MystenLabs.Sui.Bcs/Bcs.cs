namespace MystenLabs.Sui.Bcs;

/// <summary>
/// BCS (Binary Canonical Serialization) type factory and primitives for Sui/Move.
/// </summary>
public static class Bcs
{
    private const uint MaxU8 = byte.MaxValue;
    private const uint MaxU16 = ushort.MaxValue;
    private const uint MaxU32 = uint.MaxValue;
    private const byte BcsTrueByteValue = 1;
    private const byte BcsFalseByteValue = 0;
    private const int BcsBoolSerializedSizeBytes = 1;

    /// <summary>
    /// BCS type for 8-bit unsigned integer.
    /// </summary>
    public static BcsType<byte> U8(BcsTypeOptions<byte>? options = null)
    {
        return new BcsType<byte>(
            options?.Name ?? "u8",
            reader => reader.Read8(),
            (value, writer) => writer.WriteU8(value),
            _ => 1,
            value => BcsValidation.ValidateRange(value, 0, MaxU8, "u8"));
    }

    /// <summary>
    /// BCS type for 16-bit unsigned integer.
    /// </summary>
    public static BcsType<ushort> U16(BcsTypeOptions<ushort>? options = null)
    {
        return new BcsType<ushort>(
            options?.Name ?? "u16",
            reader => reader.Read16(),
            (value, writer) => writer.WriteU16(value),
            _ => 2,
            value => BcsValidation.ValidateRange((uint)value, 0, MaxU16, "u16"));
    }

    /// <summary>
    /// BCS type for 32-bit unsigned integer.
    /// </summary>
    public static BcsType<uint> U32(BcsTypeOptions<uint>? options = null)
    {
        return new BcsType<uint>(
            options?.Name ?? "u32",
            reader => reader.Read32(),
            (value, writer) => writer.WriteU32(value),
            _ => 4,
            value => BcsValidation.ValidateRange(value, 0, MaxU32, "u32"));
    }

    /// <summary>
    /// BCS type for 64-bit unsigned integer. Accepts ulong or string for large values.
    /// </summary>
    public static BcsType<ulong> U64(BcsTypeOptions<ulong>? options = null)
    {
        return new BcsType<ulong>(
            options?.Name ?? "u64",
            reader => reader.Read64(),
            (value, writer) => writer.WriteU64(value),
            _ => 8,
            _ => { });
    }

    /// <summary>
    /// BCS type for 128-bit unsigned integer. Read returns decimal string; write accepts BigInteger or string.
    /// </summary>
    public static BcsType<string> U128(BcsTypeOptions<string>? options = null)
    {
        return new BcsType<string>(
            options?.Name ?? "u128",
            reader => reader.Read128(),
            (value, writer) =>
            {
                var big = System.Numerics.BigInteger.Parse(value);
                if (big < 0 || big > MaxU128)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "u128 must be in range 0..2^128-1.");
                }

                writer.WriteU128(big);
            },
            _ => null,
            _ => { });
    }

    private static readonly System.Numerics.BigInteger MaxU128 = (System.Numerics.BigInteger.One << 128) - 1;
    private static readonly System.Numerics.BigInteger MaxU256 = (System.Numerics.BigInteger.One << 256) - 1;

    /// <summary>
    /// BCS type for 256-bit unsigned integer. Read returns decimal string; write accepts BigInteger or string.
    /// </summary>
    public static BcsType<string> U256(BcsTypeOptions<string>? options = null)
    {
        return new BcsType<string>(
            options?.Name ?? "u256",
            reader => reader.Read256(),
            (value, writer) =>
            {
                var big = System.Numerics.BigInteger.Parse(value);
                if (big < 0 || big > MaxU256)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "u256 must be in range 0..2^256-1.");
                }

                writer.WriteU256(big);
            },
            _ => null,
            _ => { });
    }

    /// <summary>
    /// BCS type for boolean (1 byte: 0 = false, 1 = true).
    /// </summary>
    public static BcsType<bool> Bool(BcsTypeOptions<bool>? options = null)
    {
        return new BcsType<bool>(
            options?.Name ?? "bool",
            reader => reader.Read8() == BcsTrueByteValue,
            (value, writer) => writer.WriteU8(value ? BcsTrueByteValue : BcsFalseByteValue),
            _ => BcsBoolSerializedSizeBytes,
            _ => { });
    }

    /// <summary>
    /// BCS type for ULEB128-encoded unsigned integer.
    /// </summary>
    public static BcsType<ulong> Uleb128(BcsTypeOptions<ulong>? options = null)
    {
        return new BcsType<ulong>(
            options?.Name ?? "uleb128",
            reader => reader.ReadUleb128(),
            (value, writer) => writer.WriteUleb128(value),
            value => global::MystenLabs.Sui.Bcs.Uleb128.Encode(value).Length,
            _ => { });
    }

    /// <summary>
    /// BCS type for a fixed-length byte array.
    /// </summary>
    /// <param name="size">Number of bytes.</param>
    /// <param name="options">Optional name and validation.</param>
    public static BcsType<byte[]> Bytes(int size, BcsTypeOptions<byte[]>? options = null)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative.");
        }

        return new BcsType<byte[]>(
            options?.Name ?? $"bytes[{size}]",
            reader => reader.ReadBytes(size),
            (value, writer) =>
            {
                if (value == null || value.Length != size)
                {
                    throw new ArgumentException($"Expected byte array of length {size}.", nameof(value));
                }

                writer.WriteBytes(value);
            },
            _ => size,
            value =>
            {
                if (value == null || value.Length != size)
                {
                    throw new ArgumentException($"Expected byte array of length {size}.", nameof(value));
                }
            });
    }

    /// <summary>
    /// BCS type for variable-length byte vector (length as ULEB128, then raw bytes).
    /// </summary>
    public static BcsType<byte[]> ByteVector(BcsTypeOptions<byte[]>? options = null)
    {
        return new BcsType<byte[]>(
            options?.Name ?? "vector<u8>",
            reader =>
            {
                int length = (int)reader.ReadUleb128();
                return reader.ReadBytes(length);
            },
            (value, writer) =>
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                writer.WriteUleb128((ulong)value.Length);
                writer.WriteBytes(value);
            },
            value => value == null ? null : global::MystenLabs.Sui.Bcs.Uleb128.Encode((ulong)value.Length).Length + value.Length,
            value =>
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
            });
    }

    /// <summary>
    /// BCS type for UTF-8 string (length as ULEB128, then UTF-8 bytes).
    /// </summary>
    public static BcsType<string> String(BcsTypeOptions<string>? options = null)
    {
        return new BcsType<string>(
            options?.Name ?? "string",
            reader =>
            {
                int length = (int)reader.ReadUleb128();
                byte[] bytes = reader.ReadBytes(length);
                return System.Text.Encoding.UTF8.GetString(bytes);
            },
            (value, writer) =>
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
                writer.WriteUleb128((ulong)bytes.Length);
                writer.WriteBytes(bytes);
            },
            value =>
            {
                if (value == null)
                {
                    return null;
                }

                int byteCount = System.Text.Encoding.UTF8.GetByteCount(value);
                return global::MystenLabs.Sui.Bcs.Uleb128.Encode((ulong)byteCount).Length + byteCount;
            },
            value =>
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
            });
    }

    /// <summary>
    /// BCS type for optional value: enum { None, Some(T) }.
    /// </summary>
    public static BcsType<T?> Option<T>(BcsType<T> type) where T : struct
    {
        return new BcsType<T?>(
            $"Option<{type.Name}>",
            reader =>
            {
                ulong index = reader.ReadUleb128();
                return index == 0 ? null : type.Read(reader);
            },
            (value, writer) =>
            {
                if (value.HasValue)
                {
                    writer.WriteUleb128(1);
                    type.Write(value.Value, writer);
                }
                else
                {
                    writer.WriteUleb128(0);
                }
            },
            value => value.HasValue ? 1 + type.GetSerializedSize(value.Value) : 1,
            _ => { });
    }

    /// <summary>
    /// BCS type for optional reference type (null = None, non-null = Some).
    /// </summary>
    public static BcsType<T?> OptionRef<T>(BcsType<T> type) where T : class
    {
        return new BcsType<T?>(
            $"Option<{type.Name}>",
            reader =>
            {
                ulong index = reader.ReadUleb128();
                return index == 0 ? null : type.Read(reader);
            },
            (value, writer) =>
            {
                if (value != null)
                {
                    writer.WriteUleb128(1);
                    type.Write(value, writer);
                }
                else
                {
                    writer.WriteUleb128(0);
                }
            },
            value => value != null ? 1 + type.GetSerializedSize(value) : 1,
            _ => { });
    }

    /// <summary>
    /// BCS type for variable-length vector of T (length as ULEB128, then each element).
    /// </summary>
    public static BcsType<T[]> Vector<T>(BcsType<T> type, BcsTypeOptions<T[]>? options = null)
    {
        return new BcsType<T[]>(
            options?.Name ?? $"vector<{type.Name}>",
            reader => reader.ReadVec((bcsReader, index, length) => type.Read(bcsReader)),
            (value, writer) =>
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                writer.WriteUleb128((ulong)value.Length);
                for (int index = 0; index < value.Length; index++)
                {
                    type.Write(value[index], writer);
                }
            },
            value =>
            {
                if (value == null)
                {
                    return null;
                }

                int sum = global::MystenLabs.Sui.Bcs.Uleb128.Encode((ulong)value.Length).Length;
                for (int index = 0; index < value.Length; index++)
                {
                    int? size = type.GetSerializedSize(value[index]);
                    if (size == null)
                    {
                        return null;
                    }

                    sum += size.Value;
                }

                return sum;
            },
            value =>
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
            });
    }

    /// <summary>
    /// BCS type for fixed-length tuple (elements serialized in order).
    /// </summary>
    public static BcsType<(T1, T2)> Tuple<T1, T2>(BcsType<T1> t1, BcsType<T2> t2)
    {
        return new BcsType<(T1, T2)>(
            $"({t1.Name}, {t2.Name})",
            reader => (t1.Read(reader), t2.Read(reader)),
            (value, writer) =>
            {
                t1.Write(value.Item1, writer);
                t2.Write(value.Item2, writer);
            },
            value =>
            {
                int? firstSize = t1.GetSerializedSize(value.Item1);
                int? secondSize = t2.GetSerializedSize(value.Item2);
                return firstSize != null && secondSize != null ? firstSize + secondSize : null;
            },
            _ => { });
    }
}

/// <summary>
/// Optional name and validation for BCS type factory methods.
/// </summary>
public sealed class BcsTypeOptions<T>
{
    /// <summary>
    /// Optional type name override.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional validation (called in addition to built-in).
    /// </summary>
    public Action<T>? Validate { get; set; }
}

/// <summary>
/// Shared validation helpers for BCS types.
/// </summary>
internal static class BcsValidation
{
    internal static void ValidateRange(uint value, uint min, uint max, string typeName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"{typeName} must be in range {min}..{max}.");
        }
    }
}
