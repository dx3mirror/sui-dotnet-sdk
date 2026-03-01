namespace MystenLabs.Sui.Transactions;

using System.Collections.Generic;
using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;

/// <summary>
/// Utility functions for transaction inputs and arguments (e.g. extracting object ID from a call argument).
/// </summary>
public static class TransactionUtils
{
    /// <summary>
    /// Returns true if the value is a valid transaction argument (GasCoin, Input, Result, or NestedResult).
    /// </summary>
    public static bool IsArgument(ArgumentValue? value)
    {
        return value is ArgumentGasCoin or ArgumentInput or ArgumentResult or ArgumentNestedResult;
    }

    /// <summary>
    /// Remaps argument references in a command using the given input and command index mappings.
    /// Used when reordering inputs or commands so that argument indices stay valid.
    /// </summary>
    /// <param name="command">The command to remap (not modified; a new command is returned).</param>
    /// <param name="inputMapping">Maps old input index to new input index.</param>
    /// <param name="commandMapping">Maps old command index to new command index (for Result and NestedResult).</param>
    /// <returns>A new command with remapped argument references.</returns>
    public static CommandValue RemapCommandArguments(
        CommandValue command,
        IReadOnlyDictionary<int, int> inputMapping,
        IReadOnlyDictionary<int, int> commandMapping)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        inputMapping ??= new Dictionary<int, int>();
        commandMapping ??= new Dictionary<int, int>();

        ArgumentValue RemapArg(ArgumentValue arg)
        {
            return arg switch
            {
                ArgumentInput input => inputMapping.TryGetValue(input.Index, out int newInput)
                    ? new ArgumentInput((ushort)newInput)
                    : throw new InvalidOperationException($"Input index {input.Index} not found in input mapping."),
                ArgumentResult result => commandMapping.TryGetValue(result.Index, out int newCmd)
                    ? new ArgumentResult((ushort)newCmd)
                    : arg,
                ArgumentNestedResult nested when commandMapping.TryGetValue(nested.CommandIndex, out int newCmd) =>
                    new ArgumentNestedResult((ushort)newCmd, nested.ResultIndex),
                _ => arg
            };
        }

        ArgumentValue[] RemapArgs(ArgumentValue[] args)
        {
            var result = new ArgumentValue[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                result[i] = RemapArg(args[i]);
            }

            return result;
        }

        return command switch
        {
            CommandMoveCall moveCall => new CommandMoveCall(moveCall.MoveCall with
            {
                Arguments = RemapArgs(moveCall.MoveCall.Arguments)
            }),
            CommandTransferObjects transfer => new CommandTransferObjects(
                RemapArgs(transfer.Objects),
                RemapArg(transfer.Address)),
            CommandSplitCoins split => new CommandSplitCoins(
                RemapArg(split.Coin),
                RemapArgs(split.Amounts)),
            CommandMergeCoins merge => new CommandMergeCoins(
                RemapArg(merge.Destination),
                RemapArgs(merge.Sources)),
            CommandMakeMoveVec makeVec => new CommandMakeMoveVec(
                makeVec.Type,
                RemapArgs(makeVec.Elements)),
            CommandUpgrade upgrade => new CommandUpgrade(
                upgrade.Modules,
                upgrade.Dependencies,
                upgrade.Package,
                RemapArg(upgrade.Ticket)),
            CommandPublish publish => publish,
            _ => command
        };
    }
    /// <summary>
    /// Extracts the normalized object ID from a string (address) or from an object <see cref="CallArg"/>.
    /// </summary>
    /// <param name="arg">Either an object ID string (normalized) or a call argument that references an object (ImmOrOwned, Shared, or Receiving).</param>
    /// <returns>Normalized object ID (0x + 64 hex), or <c>null</c> if the argument is not an object reference (e.g. Pure or FundsWithdrawal).</returns>
    public static string? GetIdFromCallArg(string? arg)
    {
        if (string.IsNullOrEmpty(arg))
        {
            return null;
        }

        return SuiAddress.Normalize(arg.AsSpan());
    }

    /// <summary>
    /// Extracts the normalized object ID from a call argument that references an object.
    /// </summary>
    /// <param name="arg">Call argument (Pure, Object, or FundsWithdrawal).</param>
    /// <returns>Normalized object ID for Object variants; <c>null</c> for Pure or FundsWithdrawal.</returns>
    public static string? GetIdFromCallArg(CallArg? arg)
    {
        if (arg == null)
        {
            return null;
        }

        if (arg is CallArgUnresolvedObject unresolved)
        {
            return unresolved.ObjectId;
        }

        if (arg is not CallArgObject objectArg)
        {
            return null;
        }

        return objectArg.Value switch
        {
            ObjectArgImmOrOwned immOrOwned => immOrOwned.Value.ObjectId,
            ObjectArgShared shared => shared.Value.ObjectId,
            ObjectArgReceiving receiving => receiving.Value.ObjectId,
            _ => null
        };
    }
}
