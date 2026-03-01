namespace MystenLabs.Sui.Transactions;

using System.Text.Json;
using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.JsonRpc.Models;

/// <summary>
/// Options for building or serializing a transaction (e.g. client for resolution, or only transaction kind).
/// </summary>
public class BuildTransactionOptions
{
    /// <summary>
    /// If set, used by the default resolution plugin to fetch object details (version, digest) for unresolved object inputs.
    /// </summary>
    public SuiClient? Client { get; set; }

    /// <summary>
    /// Optional cache for object metadata; used by the resolution plugin before fetching from the client.
    /// </summary>
    public IObjectCache? ObjectCache { get; set; }

    /// <summary>
    /// When true, build only the transaction kind (programmable transaction bytes) without sender, gas, expiration.
    /// Used for signing with external signers.
    /// </summary>
    public bool OnlyTransactionKind { get; set; }
}

/// <summary>
/// Options for serializing a transaction (extends build options; can include supported intents for partial serialization).
/// </summary>
public sealed class SerializeTransactionOptions : BuildTransactionOptions
{
    /// <summary>
    /// Intent names that are allowed to remain unresolved during serialization (e.g. for wallet support).
    /// </summary>
    public IReadOnlyList<string>? SupportedIntents { get; set; }
}

/// <summary>
/// Plugin that runs during transaction preparation (e.g. to resolve unresolved inputs or set gas).
/// Must call <paramref name="next"/> once and await it.
/// </summary>
/// <param name="data">Mutable transaction data (inputs, commands, gas, etc.).</param>
/// <param name="options">Build/serialize options (e.g. client).</param>
/// <param name="next">Continue the pipeline; must be invoked exactly once.</param>
public delegate Task TransactionPlugin(
    ITransactionDataView data,
    BuildTransactionOptions options,
    Func<Task> next);

/// <summary>
/// Read/write view of transaction data used by plugins (inputs, commands, gas, sender, expiration).
/// </summary>
public interface ITransactionDataView
{
    /// <summary>Inputs (resolved or unresolved).</summary>
    IList<CallArg> Inputs { get; }

    /// <summary>Commands.</summary>
    IList<CommandValue> Commands { get; }

    /// <summary>Gas data (payment, owner, price, budget).</summary>
    GasData? GasData { get; set; }

    /// <summary>Sender address.</summary>
    string? Sender { get; set; }

    /// <summary>Expiration.</summary>
    TransactionExpirationValue? Expiration { get; set; }
}

/// <summary>
/// Helpers for transaction resolution (unresolved inputs → resolved).
/// </summary>
public static class TransactionResolvingHelpers
{
    /// <summary>
    /// Returns true if the transaction data has unresolved inputs (UnresolvedObject or UnresolvedPure)
    /// or missing gas/sender when not onlyTransactionKind.
    /// </summary>
    public static bool NeedsTransactionResolution(ITransactionDataView data, BuildTransactionOptions options)
    {
        if (data?.Inputs == null)
        {
            return false;
        }

        foreach (CallArg input in data.Inputs)
        {
            if (input is CallArgUnresolvedObject or CallArgUnresolvedPure or CallArgUnresolvedCoinWithBalance)
            {
                return true;
            }
        }

        if (!options.OnlyTransactionKind)
        {
            if (data.GasData == null || string.IsNullOrEmpty(data.GasData.Owner) || data.GasData.Payment == null)
            {
                return true;
            }

            if (data.GasData.Payment.Length == 0 && data.Expiration == null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts cached object metadata to a resolved <see cref="CallArgObject"/> (owned, shared, or receiving).
    /// </summary>
    /// <param name="entry">Cached entry (from RPC or cache).</param>
    /// <param name="mutable">For shared objects, whether the reference is mutable; ignored for owned.</param>
    /// <returns>Resolved call argument.</returns>
    public static CallArgObject ToCallArgObject(ObjectCacheEntry entry, bool mutable = true)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (!ulong.TryParse(entry.Version, out ulong version))
        {
            throw new ArgumentException($"Invalid version: {entry.Version}.", nameof(entry));
        }

        if (string.IsNullOrEmpty(entry.Digest))
        {
            throw new ArgumentException("Digest is required.", nameof(entry));
        }

        if (!string.IsNullOrEmpty(entry.InitialSharedVersion) && ulong.TryParse(entry.InitialSharedVersion, out ulong initialSharedVersion))
        {
            var sharedRef = new SharedObjectRef(entry.ObjectId, initialSharedVersion, mutable);
            return new CallArgObject(new ObjectArgShared(sharedRef));
        }

        var objectRef = new SuiObjectRef(entry.ObjectId, version, entry.Digest);
        return new CallArgObject(new ObjectArgImmOrOwned(objectRef));
    }

    /// <summary>
    /// Builds an <see cref="ObjectCacheEntry"/> from RPC object data (e.g. from sui_getObject).
    /// </summary>
    /// <param name="data">Object data from <see cref="SuiObjectResponse.Data"/>.</param>
    /// <returns>Cache entry with objectId, version, digest, and optional initialSharedVersion for shared objects.</returns>
    public static ObjectCacheEntry ObjectCacheEntryFromSuiObjectData(SuiObjectData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        string? initialSharedVersion = null;
        if (data.Owner is JsonElement ownerElement)
        {
            if (ownerElement.TryGetProperty("Shared", out JsonElement shared))
            {
                if (shared.TryGetProperty("initial_shared_version", out JsonElement isv))
                {
                    initialSharedVersion = isv.ValueKind == JsonValueKind.String ? isv.GetString() : isv.GetRawText();
                }
                else if (shared.TryGetProperty("initialSharedVersion", out JsonElement isv2))
                {
                    initialSharedVersion = isv2.ValueKind == JsonValueKind.String ? isv2.GetString() : isv2.GetRawText();
                }
            }
        }

        return new ObjectCacheEntry(
            data.ObjectId ?? "",
            data.Version ?? "0",
            data.Digest ?? "",
            null,
            initialSharedVersion);
    }

    /// <summary>
    /// Tries to serialize an <see cref="CallArgUnresolvedPure"/> value to bytes (u64, address, bool, u8, string).
    /// Returns null if the value type is not supported for auto-serialization.
    /// </summary>
    public static CallArgPure? TryResolveUnresolvedPure(CallArgUnresolvedPure unresolved)
    {
        if (unresolved?.Value == null)
        {
            return null;
        }

        object value = unresolved.Value;
        if (value is ulong u64)
        {
            return new CallArgPure(Pure.SerializeU64(u64));
        }

        if (value is long i64 && i64 >= 0)
        {
            return new CallArgPure(Pure.SerializeU64((ulong)i64));
        }

        if (value is bool b)
        {
            return new CallArgPure(Pure.SerializeBool(b));
        }

        if (value is byte u8)
        {
            return new CallArgPure(Pure.SerializeU8(u8));
        }

        if (value is string str)
        {
            if (str.AsSpan().Trim().Length >= 2 && str.TrimStart().StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Pure.Address(str);
            }

            return new CallArgPure(Pure.SerializeString(str));
        }

        return null;
    }

    /// <summary>
    /// Default resolution plugin: resolves <see cref="CallArgUnresolvedObject"/> using optional
    /// <see cref="IObjectCache"/> and then <see cref="SuiClient"/> from options. Replaces each unresolved
    /// input with <see cref="CallArgObject"/> (from cache or RPC). The plugin must invoke the next delegate once.
    /// </summary>
    public static TransactionPlugin ResolveTransactionPlugin()
    {
        return async (data, options, next) =>
        {
            if (data?.Inputs == null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            IObjectCache? cache = options.ObjectCache;
            SuiClient? client = options.Client;

            for (int index = 0; index < data.Inputs.Count; index++)
            {
                if (data.Inputs[index] is not CallArgUnresolvedObject unresolved)
                {
                    continue;
                }

                string objectId = unresolved.ObjectId;
                ObjectCacheEntry? entry = cache?.GetObject(objectId);

                if (entry == null && client != null)
                {
                    SuiObjectResponse response = await client.GetObjectAsync(objectId).ConfigureAwait(false);
                    if (response.Error != null || response.Data == null)
                    {
                        throw new InvalidOperationException(
                            $"Failed to resolve object {objectId}: {response.Error?.Message ?? "no data"}.");
                    }

                    entry = ObjectCacheEntryFromSuiObjectData(response.Data);
                }

                if (entry == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve object {objectId}. Set BuildTransactionOptions.ObjectCache or BuildTransactionOptions.Client and ensure the object is available.");
                }

                bool mutable = unresolved.Mutable ?? true;
                data.Inputs[index] = ToCallArgObject(entry, mutable);
            }

            for (int index = 0; index < data.Inputs.Count; index++)
            {
                if (data.Inputs[index] is CallArgUnresolvedPure unresolvedPure)
                {
                    CallArgPure? resolved = TryResolveUnresolvedPure(unresolvedPure);
                    if (resolved == null)
                    {
                        throw new InvalidOperationException(
                            $"UnresolvedPure at input index {index} has a value type that cannot be auto-serialized. Use Pure(byte[]) or Pure(CallArgPure) for custom types.");
                    }

                    data.Inputs[index] = resolved;
                }
            }

            await next().ConfigureAwait(false);
        };
    }

    private const string DefaultSuiCoinType = "0x2::sui::SUI";

    /// <summary>
    /// Plugin that resolves <see cref="CallArgUnresolvedCoinWithBalance"/> inputs by fetching coins for the transaction sender
    /// and replacing each with a <see cref="CallArgObject"/> (first coin with balance &gt;= required amount).
    /// Requires <see cref="BuildTransactionOptions.Client"/> and sender set on the transaction data. Run before or with <see cref="ResolveTransactionPlugin"/>.
    /// </summary>
    public static TransactionPlugin ResolveCoinBalancePlugin()
    {
        return async (data, options, next) =>
        {
            if (data?.Inputs == null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            string? sender = data.Sender;
            SuiClient? client = options.Client;
            if (string.IsNullOrEmpty(sender) || client == null)
            {
                for (int index = 0; index < data.Inputs.Count; index++)
                {
                    if (data.Inputs[index] is CallArgUnresolvedCoinWithBalance)
                    {
                        throw new InvalidOperationException(
                            "Cannot resolve CoinWithBalance: set BuildTransactionOptions.Client and transaction sender (e.g. SetSender) before PrepareForSerializationAsync.");
                    }
                }

                await next().ConfigureAwait(false);
                return;
            }

            for (int index = 0; index < data.Inputs.Count; index++)
            {
                if (data.Inputs[index] is not CallArgUnresolvedCoinWithBalance coinWithBalance)
                {
                    continue;
                }

                ulong required = coinWithBalance.Balance;
                string? coinType = coinWithBalance.CoinType ?? DefaultSuiCoinType;
                string? cursor = null;
                const uint pageSize = 50;
                ObjectCacheEntry? chosen = null;

                while (true)
                {
                    PaginatedCoinsResponse page = await client.GetCoinsAsync(sender, coinType, cursor, pageSize).ConfigureAwait(false);
                    if (page.Data == null || page.Data.Length == 0)
                    {
                        break;
                    }

                    foreach (SuiCoinObject coin in page.Data)
                    {
                        if (string.IsNullOrEmpty(coin.CoinObjectId) || string.IsNullOrEmpty(coin.Version) || string.IsNullOrEmpty(coin.Digest) || string.IsNullOrEmpty(coin.Balance))
                        {
                            continue;
                        }

                        if (!ulong.TryParse(coin.Balance, out ulong balance))
                        {
                            continue;
                        }

                        if (balance >= required)
                        {
                            chosen = new ObjectCacheEntry(
                                coin.CoinObjectId,
                                coin.Version,
                                coin.Digest,
                                sender,
                                null);
                            break;
                        }
                    }

                    if (chosen != null)
                    {
                        break;
                    }

                    if (!page.HasNextPage || string.IsNullOrEmpty(page.NextCursor))
                    {
                        break;
                    }

                    cursor = page.NextCursor;
                }

                if (chosen == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve CoinWithBalance: no coin with balance >= {required} (type {coinType}) found for sender {sender}. Ensure the sender has sufficient balance.");
                }

                options.ObjectCache?.AddObject(chosen);
                data.Inputs[index] = ToCallArgObject(chosen, true);
            }

            await next().ConfigureAwait(false);
        };
    }
}
