namespace MystenLabs.Sui.Bcs;

/// <summary>
/// BCS serialization for <see cref="ArgumentValue"/> (enum: GasCoin, Input(u16), Result(u16), NestedResult(u16,u16)).
/// </summary>
public static class ArgumentBcs
{
    private const int VariantGasCoin = 0;
    private const int VariantInput = 1;
    private const int VariantResult = 2;
    private const int VariantNestedResult = 3;

    /// <summary>
    /// BCS type for Argument.
    /// </summary>
    public static BcsType<ArgumentValue> Argument { get; } = new BcsType<ArgumentValue>(
        "Argument",
        reader =>
        {
            ulong index = reader.ReadUleb128();
            return index switch
            {
                VariantGasCoin => new ArgumentGasCoin(),
                VariantInput => new ArgumentInput(reader.Read16()),
                VariantResult => new ArgumentResult(reader.Read16()),
                VariantNestedResult => new ArgumentNestedResult(reader.Read16(), reader.Read16()),
                _ => throw new InvalidOperationException($"Unknown Argument variant index: {index}.")
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
                case ArgumentGasCoin:
                    writer.WriteUleb128(VariantGasCoin);
                    break;
                case ArgumentInput input:
                    writer.WriteUleb128(VariantInput);
                    writer.WriteU16(input.Index);
                    break;
                case ArgumentResult result:
                    writer.WriteUleb128(VariantResult);
                    writer.WriteU16(result.Index);
                    break;
                case ArgumentNestedResult nested:
                    writer.WriteUleb128(VariantNestedResult);
                    writer.WriteU16(nested.CommandIndex);
                    writer.WriteU16(nested.ResultIndex);
                    break;
                default:
                    throw new ArgumentException($"Unknown Argument type: {value.GetType().Name}.", nameof(value));
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
