namespace MystenLabs.Sui.Bcs;

using MystenLabs.Sui.Utils;

/// <summary>
/// Call argument for Move: pure bytes, object reference, or funds withdrawal.
/// </summary>
public abstract record CallArg;

/// <summary>
/// Pure argument: BCS-serialized bytes (e.g. for primitive arguments). Often represented as Base64.
/// </summary>
/// <param name="Bytes">Raw serialized bytes. When passing from RPC/API, use Base64 string and decode before construction or use the BCS type which accepts Base64.</param>
public sealed record CallArgPure(byte[] Bytes) : CallArg;

/// <summary>
/// Object argument: reference to an owned, shared, or receiving object.
/// </summary>
public sealed record CallArgObject(ObjectArg Value) : CallArg;

/// <summary>
/// Funds withdrawal argument (reservation, type, sender/sponsor).
/// </summary>
public sealed record CallArgFundsWithdrawal(FundsWithdrawal Value) : CallArg;

/// <summary>
/// Builder-only: object input not yet resolved (has object ID; version/digest filled by resolver or cache).
/// Not BCS-serialized; must be replaced with <see cref="CallArgObject"/> before serialization.
/// </summary>
/// <param name="ObjectId">Object ID (normalized).</param>
/// <param name="Version">Object version (optional; set by resolver).</param>
/// <param name="Digest">Object digest Base58 (optional; set by resolver).</param>
/// <param name="InitialSharedVersion">Initial shared version for shared objects (optional).</param>
/// <param name="Mutable">Whether used as mutable (optional; for shared objects).</param>
public sealed record CallArgUnresolvedObject(
    string ObjectId,
    string? Version = null,
    string? Digest = null,
    string? InitialSharedVersion = null,
    bool? Mutable = null) : CallArg;

/// <summary>
/// Builder-only: pure input not yet serialized (value to be serialized at resolution time).
/// Not BCS-serialized; must be replaced with <see cref="CallArgPure"/> before serialization.
/// </summary>
/// <param name="Value">Value to serialize (e.g. number, address string); resolver uses schema or type to produce bytes.</param>
public sealed record CallArgUnresolvedPure(object? Value) : CallArg;

/// <summary>
/// Builder-only: input that must be resolved to a coin object with at least the given balance (intent SuiIntents.CoinWithBalance).
/// Not BCS-serialized; must be replaced with <see cref="CallArgObject"/> by the coin-balance resolver before serialization.
/// </summary>
/// <param name="Balance">Minimum balance required (in MIST for SUI).</param>
/// <param name="CoinType">Optional coin type (e.g. "0x2::sui::SUI"). Defaults to SUI when null.</param>
public sealed record CallArgUnresolvedCoinWithBalance(ulong Balance, string? CoinType = null) : CallArg;

/// <summary>
/// BCS serialization for <see cref="CallArg"/> (enum: Pure, Object, FundsWithdrawal).
/// Pure variant: struct with single field bytes (length-prefixed byte vector); string input/output as Base64.
/// </summary>
public static class CallArgBcs
{
    private const int VariantPure = 0;
    private const int VariantObject = 1;
    private const int VariantFundsWithdrawal = 2;

    private static BcsType<byte[]> PureBytes { get; } = Bcs.ByteVector();

    /// <summary>
    /// BCS type for CallArg (Pure, Object, and FundsWithdrawal).
    /// </summary>
    public static BcsType<CallArg> CallArg { get; } = new BcsType<CallArg>(
        "CallArg",
        reader =>
        {
            ulong index = reader.ReadUleb128();
            return index switch
            {
                VariantPure => new CallArgPure(PureBytes.Read(reader)),
                VariantObject => new CallArgObject(ObjectArgBcs.ObjectArg.Read(reader)),
                VariantFundsWithdrawal => new CallArgFundsWithdrawal(FundsWithdrawalBcs.FundsWithdrawal.Read(reader)),
                _ => throw new InvalidOperationException($"Unknown CallArg variant index: {index}.")
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
                case CallArgPure pure:
                    writer.WriteUleb128(VariantPure);
                    PureBytes.Write(pure.Bytes, writer);
                    break;
                case CallArgObject objectArg:
                    writer.WriteUleb128(VariantObject);
                    ObjectArgBcs.ObjectArg.Write(objectArg.Value, writer);
                    break;
                case CallArgFundsWithdrawal fundsWithdrawal:
                    writer.WriteUleb128(VariantFundsWithdrawal);
                    FundsWithdrawalBcs.FundsWithdrawal.Write(fundsWithdrawal.Value, writer);
                    break;
                case CallArgUnresolvedObject:
                case CallArgUnresolvedPure:
                case CallArgUnresolvedCoinWithBalance:
                    throw new ArgumentException(
                        "Unresolved call arguments must be resolved before BCS serialization. Run the transaction resolution pipeline.",
                        nameof(value));
                default:
                    throw new ArgumentException($"Unsupported CallArg type: {value.GetType().Name}.", nameof(value));
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

    /// <summary>
    /// Creates a pure call argument from Base64-encoded bytes (e.g. from RPC or JSON).
    /// </summary>
    /// <param name="base64">Base64-encoded serialized value.</param>
    /// <returns>CallArgPure with decoded bytes.</returns>
    public static CallArgPure PureFromBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64))
        {
            throw new ArgumentNullException(nameof(base64));
        }

        return new CallArgPure(Base64.Decode(base64.AsSpan()));
    }

    /// <summary>
    /// Converts pure argument bytes to Base64 string.
    /// </summary>
    public static string PureToBase64(CallArgPure pure)
    {
        if (pure == null || pure.Bytes == null)
        {
            throw new ArgumentNullException(nameof(pure));
        }

        return Base64.Encode(pure.Bytes);
    }

    /// <summary>
    /// Returns true if the call argument is resolved (Pure, Object, or FundsWithdrawal) and can be BCS-serialized.
    /// </summary>
    public static bool IsResolved(CallArg? arg)
    {
        return arg is CallArgPure or CallArgObject or CallArgFundsWithdrawal;
    }
}
