namespace MystenLabs.Sui.Transactions;

using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;

/// <summary>
/// Factory methods for creating <see cref="CallArg"/> inputs (pure bytes, object refs, funds withdrawal).
/// Use these to build inputs for programmable transactions; addresses are normalized.
/// </summary>
public static class Inputs
{
    /// <summary>
    /// Creates a pure call argument from BCS-serialized bytes.
    /// </summary>
    /// <param name="bytes">Serialized BCS bytes (e.g. from <see cref="Pure.SerializeU64"/> or <see cref="Pure.SerializeAddress"/>).</param>
    /// <returns>Call argument for use as a transaction input.</returns>
    public static CallArgPure Pure(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        return new CallArgPure(bytes);
    }

    /// <summary>
    /// Creates an owned or immutable object call argument from an object reference.
    /// </summary>
    /// <param name="objectId">Object ID (normalized to 0x + 64 hex).</param>
    /// <param name="version">Object version.</param>
    /// <param name="digest">Object digest (Base58).</param>
    /// <returns>Call argument for use as a transaction input.</returns>
    public static CallArgObject ObjectRef(string objectId, ulong version, string digest)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            throw new ArgumentNullException(nameof(objectId));
        }

        if (string.IsNullOrEmpty(digest))
        {
            throw new ArgumentNullException(nameof(digest));
        }

        string normalized = SuiAddress.Normalize(objectId.AsSpan());
        var reference = new SuiObjectRef(normalized, version, digest);
        return new CallArgObject(new ObjectArgImmOrOwned(reference));
    }

    /// <summary>
    /// Creates a shared object call argument.
    /// </summary>
    /// <param name="objectId">Object ID (normalized).</param>
    /// <param name="initialSharedVersion">Version at which the object was shared.</param>
    /// <param name="mutable">Whether the shared reference allows mutation.</param>
    /// <returns>Call argument for use as a transaction input.</returns>
    public static CallArgObject SharedObjectRef(string objectId, ulong initialSharedVersion, bool mutable)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            throw new ArgumentNullException(nameof(objectId));
        }

        string normalized = SuiAddress.Normalize(objectId.AsSpan());
        var reference = new SharedObjectRef(normalized, initialSharedVersion, mutable);
        return new CallArgObject(new ObjectArgShared(reference));
    }

    /// <summary>
    /// Creates a receiving object call argument (e.g. for receiving a coin).
    /// </summary>
    /// <param name="objectId">Object ID (normalized).</param>
    /// <param name="version">Object version.</param>
    /// <param name="digest">Object digest (Base58).</param>
    /// <returns>Call argument for use as a transaction input.</returns>
    public static CallArgObject ReceivingRef(string objectId, ulong version, string digest)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            throw new ArgumentNullException(nameof(objectId));
        }

        if (string.IsNullOrEmpty(digest))
        {
            throw new ArgumentNullException(nameof(digest));
        }

        string normalized = SuiAddress.Normalize(objectId.AsSpan());
        var reference = new SuiObjectRef(normalized, version, digest);
        return new CallArgObject(new ObjectArgReceiving(reference));
    }

    /// <summary>
    /// Creates a funds withdrawal call argument (e.g. for gas payment from address balance).
    /// </summary>
    /// <param name="amount">Maximum amount to withdraw (u64).</param>
    /// <param name="balanceType">Balance type tag string (e.g. "0x2::sui::SUI"). Defaults to SUI.</param>
    /// <param name="fromSponsor">If true, withdraw from sponsor; otherwise from sender.</param>
    /// <returns>Call argument for use as a transaction input.</returns>
    public static CallArgFundsWithdrawal FundsWithdrawal(
        ulong amount,
        string? balanceType = null,
        bool fromSponsor = false)
    {
        string typeTag = string.IsNullOrWhiteSpace(balanceType) ? "0x2::sui::SUI" : balanceType;
        TypeTagValue typeArg = TypeTagSerializer.ParseFromStr(typeTag, normalizeAddress: true);
        var reservation = new Reservation(amount);
        var withdrawalType = new WithdrawalType(typeArg);
        WithdrawFrom withdrawFrom = fromSponsor ? new WithdrawFromSponsor() : new WithdrawFromSender();
        var value = new FundsWithdrawal(reservation, withdrawalType, withdrawFrom);
        return new CallArgFundsWithdrawal(value);
    }
}
