namespace MystenLabs.Sui.Bcs;

/// <summary>
/// BCS serialization for <see cref="ProgrammableMoveCall"/> (struct: package, module, function, typeArguments, arguments).
/// </summary>
public static class ProgrammableMoveCallBcs
{
    /// <summary>
    /// BCS type for ProgrammableMoveCall.
    /// </summary>
    public static BcsType<ProgrammableMoveCall> ProgrammableMoveCall { get; } = new BcsType<ProgrammableMoveCall>(
        "ProgrammableMoveCall",
        reader =>
        {
            string package = SuiBcsTypes.Address.Read(reader);
            string module = Bcs.String().Read(reader);
            string function = Bcs.String().Read(reader);
            TypeTagValue[] typeArguments = Bcs.Vector(TypeTagBcs.TypeTag).Read(reader);
            ArgumentValue[] arguments = Bcs.Vector(ArgumentBcs.Argument).Read(reader);
            return new ProgrammableMoveCall(package, module, function, typeArguments, arguments);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            SuiBcsTypes.Address.Write(value.Package, writer);
            Bcs.String().Write(value.Module, writer);
            Bcs.String().Write(value.Function, writer);
            Bcs.Vector(TypeTagBcs.TypeTag).Write(value.TypeArguments, writer);
            Bcs.Vector(ArgumentBcs.Argument).Write(value.Arguments, writer);
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
