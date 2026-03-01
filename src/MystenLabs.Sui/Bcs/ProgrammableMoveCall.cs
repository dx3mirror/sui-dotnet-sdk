namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Move call: package::module::function with type arguments and arguments.
/// </summary>
/// <param name="Package">Package address.</param>
/// <param name="Module">Module name.</param>
/// <param name="Function">Function name.</param>
/// <param name="TypeArguments">Type parameters (e.g. for generic functions).</param>
/// <param name="Arguments">Transaction arguments (inputs, results, gas coin).</param>
public sealed record ProgrammableMoveCall(
    string Package,
    string Module,
    string Function,
    TypeTagValue[] TypeArguments,
    ArgumentValue[] Arguments);
