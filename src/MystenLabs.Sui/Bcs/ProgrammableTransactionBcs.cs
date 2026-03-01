namespace MystenLabs.Sui.Bcs;

/// <summary>
/// BCS serialization for <see cref="ProgrammableTransaction"/> (struct: inputs, commands).
/// </summary>
public static class ProgrammableTransactionBcs
{
    private static BcsType<CallArg[]> CallArgVector { get; } = Bcs.Vector(CallArgBcs.CallArg);
    private static BcsType<CommandValue[]> CommandVector { get; } = Bcs.Vector(CommandBcs.Command);

    /// <summary>
    /// BCS type for ProgrammableTransaction.
    /// </summary>
    public static BcsType<ProgrammableTransaction> ProgrammableTransaction { get; } = new BcsType<ProgrammableTransaction>(
        "ProgrammableTransaction",
        reader =>
        {
            CallArg[] inputs = CallArgVector.Read(reader);
            CommandValue[] commands = CommandVector.Read(reader);
            return new ProgrammableTransaction(inputs, commands);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            CallArgVector.Write(value.Inputs, writer);
            CommandVector.Write(value.Commands, writer);
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
