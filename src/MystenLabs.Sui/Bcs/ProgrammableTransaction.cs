namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Programmable transaction: inputs (CallArg) and commands.
/// </summary>
/// <param name="Inputs">Transaction inputs (pure bytes, objects, or funds withdrawal).</param>
/// <param name="Commands">Commands to execute in order.</param>
public sealed record ProgrammableTransaction(CallArg[] Inputs, CommandValue[] Commands);
