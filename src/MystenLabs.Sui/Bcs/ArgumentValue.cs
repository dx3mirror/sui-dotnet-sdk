namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Transaction argument reference: GasCoin, input index, result index, or nested result (command index + result index).
/// </summary>
public abstract record ArgumentValue;

/// <summary>
/// Gas coin argument.
/// </summary>
public sealed record ArgumentGasCoin : ArgumentValue;

/// <summary>
/// Reference to an input by index (u16).
/// </summary>
public sealed record ArgumentInput(ushort Index) : ArgumentValue;

/// <summary>
/// Reference to a command result by index (u16).
/// </summary>
public sealed record ArgumentResult(ushort Index) : ArgumentValue;

/// <summary>
/// Reference to a nested result (command index, result index).
/// </summary>
public sealed record ArgumentNestedResult(ushort CommandIndex, ushort ResultIndex) : ArgumentValue;
