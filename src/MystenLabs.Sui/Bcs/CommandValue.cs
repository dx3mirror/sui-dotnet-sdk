namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Transaction command (MoveCall, TransferObjects, SplitCoins, etc.).
/// </summary>
public abstract record CommandValue;

/// <summary>
/// Move call command.
/// </summary>
public sealed record CommandMoveCall(ProgrammableMoveCall MoveCall) : CommandValue;

/// <summary>
/// Transfer objects to an address.
/// </summary>
public sealed record CommandTransferObjects(ArgumentValue[] Objects, ArgumentValue Address) : CommandValue;

/// <summary>
/// Split a coin into amounts.
/// </summary>
public sealed record CommandSplitCoins(ArgumentValue Coin, ArgumentValue[] Amounts) : CommandValue;

/// <summary>
/// Merge coins into destination.
/// </summary>
public sealed record CommandMergeCoins(ArgumentValue Destination, ArgumentValue[] Sources) : CommandValue;

/// <summary>
/// Publish Move modules.
/// </summary>
/// <param name="Modules">Module bytes (e.g. Base64 or raw). Stored as byte arrays for BCS.</param>
/// <param name="Dependencies">Package IDs (addresses).</param>
public sealed record CommandPublish(byte[][] Modules, string[] Dependencies) : CommandValue;

/// <summary>
/// Build a vector of objects (optional type, elements).
/// </summary>
/// <param name="Type">Optional type tag for the vector; null if not specified.</param>
/// <param name="Elements">Element arguments.</param>
public sealed record CommandMakeMoveVec(TypeTagValue? Type, ArgumentValue[] Elements) : CommandValue;

/// <summary>
/// Upgrade a package.
/// </summary>
public sealed record CommandUpgrade(
    byte[][] Modules,
    string[] Dependencies,
    string Package,
    ArgumentValue Ticket) : CommandValue;
