namespace MystenLabs.Sui.Bcs;

/// <summary>
/// BCS serialization for <see cref="TypeTagValue"/>. Enum order: bool, u8, u64, u128, address, signer, vector, struct, u16, u32, u256.
/// </summary>
public static class TypeTagBcs
{
    private const int VariantBool = 0;
    private const int VariantU8 = 1;
    private const int VariantU64 = 2;
    private const int VariantU128 = 3;
    private const int VariantAddress = 4;
    private const int VariantSigner = 5;
    private const int VariantVector = 6;
    private const int VariantStruct = 7;
    private const int VariantU16 = 8;
    private const int VariantU32 = 9;
    private const int VariantU256 = 10;

    private static BcsType<TypeTagValue>? _typeTag;

    /// <summary>
    /// BCS type for TypeTag (recursive: vector and struct refer back to TypeTag).
    /// </summary>
    public static BcsType<TypeTagValue> TypeTag => _typeTag ??= BuildTypeTag(() => TypeTag);

    private static BcsType<TypeTagValue> BuildTypeTag(Func<BcsType<TypeTagValue>> getSelf)
    {
        return new BcsType<TypeTagValue>(
            "TypeTag",
            reader =>
            {
                ulong index = reader.ReadUleb128();
                return index switch
                {
                    VariantBool => new TypeTagBool(),
                    VariantU8 => new TypeTagU8(),
                    VariantU64 => new TypeTagU64(),
                    VariantU128 => new TypeTagU128(),
                    VariantAddress => new TypeTagAddress(),
                    VariantSigner => new TypeTagSigner(),
                    VariantVector => new TypeTagVector(getSelf().Read(reader)),
                    VariantStruct => new TypeTagStruct(StructTagBcs.StructTag.Read(reader)),
                    VariantU16 => new TypeTagU16(),
                    VariantU32 => new TypeTagU32(),
                    VariantU256 => new TypeTagU256(),
                    _ => throw new InvalidOperationException($"Unknown TypeTag variant index: {index}.")
                };
            },
            (value, writer) =>
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                switch (value)
                {
                    case TypeTagBool:
                        writer.WriteUleb128(VariantBool);
                        break;
                    case TypeTagU8:
                        writer.WriteUleb128(VariantU8);
                        break;
                    case TypeTagU64:
                        writer.WriteUleb128(VariantU64);
                        break;
                    case TypeTagU128:
                        writer.WriteUleb128(VariantU128);
                        break;
                    case TypeTagAddress:
                        writer.WriteUleb128(VariantAddress);
                        break;
                    case TypeTagSigner:
                        writer.WriteUleb128(VariantSigner);
                        break;
                    case TypeTagVector vector:
                        writer.WriteUleb128(VariantVector);
                        getSelf().Write(vector.Inner, writer);
                        break;
                    case TypeTagStruct structTag:
                        writer.WriteUleb128(VariantStruct);
                        StructTagBcs.StructTag.Write(structTag.Struct, writer);
                        break;
                    case TypeTagU16:
                        writer.WriteUleb128(VariantU16);
                        break;
                    case TypeTagU32:
                        writer.WriteUleb128(VariantU32);
                        break;
                    case TypeTagU256:
                        writer.WriteUleb128(VariantU256);
                        break;
                    default:
                        throw new ArgumentException($"Unknown TypeTag type: {value.GetType().Name}.", nameof(value));
                }
            },
            _ => null,
            value =>
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
            });
    }
}
