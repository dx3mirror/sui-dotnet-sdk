namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Gas data: payment objects, owner, gas price, and budget.
/// </summary>
/// <param name="Payment">Gas payment object references.</param>
/// <param name="Owner">Gas object owner address.</param>
/// <param name="Price">Gas price.</param>
/// <param name="Budget">Gas budget.</param>
public sealed record GasData(SuiObjectRef[] Payment, string Owner, ulong Price, ulong Budget);

/// <summary>
/// Valid during: epoch and timestamp bounds, chain, nonce.
/// </summary>
public sealed record ValidDuring(
    ulong? MinEpoch,
    ulong? MaxEpoch,
    ulong? MinTimestamp,
    ulong? MaxTimestamp,
    string Chain,
    uint Nonce);

/// <summary>
/// Transaction expiration: None, Epoch, or ValidDuring.
/// </summary>
public abstract record TransactionExpirationValue;

/// <summary>
/// No expiration.
/// </summary>
public sealed record TransactionExpirationNone : TransactionExpirationValue;

/// <summary>
/// Expire at epoch.
/// </summary>
public sealed record TransactionExpirationEpoch(ulong Epoch) : TransactionExpirationValue;

/// <summary>
/// Valid during a time window.
/// </summary>
public sealed record TransactionExpirationValidDuring(ValidDuring Value) : TransactionExpirationValue;

/// <summary>
/// Transaction kind: currently only ProgrammableTransaction is used.
/// </summary>
public abstract record TransactionKindValue;

/// <summary>
/// Programmable transaction (inputs + commands).
/// </summary>
public sealed record TransactionKindProgrammable(ProgrammableTransaction Value) : TransactionKindValue;

/// <summary>
/// Transaction data V1 (kind, sender, gas data, expiration). This is the BCS payload.
/// </summary>
public sealed record TransactionDataV1(
    TransactionKindValue Kind,
    string Sender,
    GasData GasData,
    TransactionExpirationValue Expiration);

/// <summary>
/// Transaction data (enum: V1). Root type for BCS serialization.
/// </summary>
public sealed record TransactionData(TransactionDataV1 V1);
