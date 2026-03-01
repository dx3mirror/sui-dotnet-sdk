namespace MystenLabs.Sui.Bcs;

/// <summary>
/// BCS serialization for <see cref="StructTag"/> (struct: address, module, name, typeParams).
/// </summary>
public static class StructTagBcs
{
    private static BcsType<StructTag>? _structTag;

    /// <summary>
    /// BCS type for StructTag. Depends on <see cref="TypeTagBcs.TypeTag"/> for typeParams (lazy to break recursion).
    /// </summary>
    public static BcsType<StructTag> StructTag => _structTag ??= BuildStructTag();

    private static BcsType<StructTag> BuildStructTag()
    {
        return new BcsType<StructTag>(
            "StructTag",
            reader =>
            {
                string address = SuiBcsTypes.Address.Read(reader);
                string module = Bcs.String().Read(reader);
                string name = Bcs.String().Read(reader);
                TypeTagValue[] typeParams = Bcs.Vector(TypeTagBcs.TypeTag).Read(reader);
                return new StructTag(address, module, name, typeParams);
            },
            (value, writer) =>
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                SuiBcsTypes.Address.Write(value.Address, writer);
                Bcs.String().Write(value.Module, writer);
                Bcs.String().Write(value.Name, writer);
                Bcs.Vector(TypeTagBcs.TypeTag).Write(value.TypeParams, writer);
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
