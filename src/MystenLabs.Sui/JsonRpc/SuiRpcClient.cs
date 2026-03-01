namespace MystenLabs.Sui.JsonRpc;

using System.Text.Json;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.JsonRpc.Models;
using MystenLabs.Sui.Utils;

/// <summary>
/// Sui JSON-RPC client: getObject, getBalance, executeTransactionBlock, and related methods.
/// </summary>
public sealed class SuiRpcClient
{
    private readonly HttpTransport _transport;

    /// <summary>
    /// Creates an RPC client for the given URL (and optional HttpClient).
    /// </summary>
    public SuiRpcClient(string rpcUrl, HttpClient? httpClient = null)
    {
        _transport = new HttpTransport(rpcUrl, httpClient);
    }

    /// <summary>
    /// Creates an RPC client using the given transport (e.g. for testing).
    /// </summary>
    public SuiRpcClient(HttpTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <summary>
    /// Gets an object by ID. Returns the RPC response (data or error).
    /// </summary>
    /// <param name="objectId">Object ID (0x + 64 hex chars or normalized).</param>
    /// <param name="options">Optional request options (showOwner, showType, etc.).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<SuiObjectResponse> GetObjectAsync(
        string objectId,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        string normalized = SuiAddress.Normalize(objectId.AsSpan());
        return _transport.RequestAsync<SuiObjectResponse>(
            RpcMethods.GetObject,
            [normalized, options],
            cancellationToken);
    }

    /// <summary>
    /// Gets the coin balance for an owner and optional coin type.
    /// </summary>
    /// <param name="owner">Owner address.</param>
    /// <param name="coinType">Optional coin type (e.g. 0x2::sui::SUI). Defaults to SUI.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<CoinBalance> GetBalanceAsync(
        string owner,
        string? coinType = null,
        CancellationToken cancellationToken = default)
    {
        string normalized = SuiAddress.Normalize(owner.AsSpan());
        return _transport.RequestAsync<CoinBalance>(
            RpcMethods.GetBalance,
            [normalized, coinType],
            cancellationToken);
    }

    /// <summary>
    /// Executes a signed transaction block.
    /// </summary>
    /// <param name="transactionBlock">Transaction bytes (base64) or raw bytes (will be base64-encoded).</param>
    /// <param name="signatures">One or more serialized signatures (base64).</param>
    /// <param name="options">Optional request options.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<SuiTransactionBlockResponse> ExecuteTransactionBlockAsync(
        string transactionBlock,
        string[] signatures,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(transactionBlock))
        {
            throw new ArgumentNullException(nameof(transactionBlock));
        }

        if (signatures == null || signatures.Length == 0)
        {
            throw new ArgumentException("At least one signature is required.", nameof(signatures));
        }

        return _transport.RequestAsync<SuiTransactionBlockResponse>(
            RpcMethods.ExecuteTransactionBlock,
            [transactionBlock, signatures, options],
            cancellationToken);
    }

    /// <summary>
    /// Executes a signed transaction block (transaction and signatures as bytes; encoded to base64).
    /// </summary>
    public Task<SuiTransactionBlockResponse> ExecuteTransactionBlockAsync(
        byte[] transactionBlock,
        byte[][] signatures,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        string transactionBase64 = Base64.Encode(transactionBlock);
        string[] signaturesBase64 = new string[signatures.Length];
        for (int index = 0; index < signatures.Length; index++)
        {
            signaturesBase64[index] = Base64.Encode(signatures[index]);
        }

        return ExecuteTransactionBlockAsync(transactionBase64, signaturesBase64, options, cancellationToken);
    }

    /// <summary>
    /// Gets the reference gas price (for building transactions). Server may return number or string.
    /// </summary>
    public async Task<string> GetReferenceGasPriceAsync(CancellationToken cancellationToken = default)
    {
        object? raw = await _transport
            .RequestAsync<object>(RpcMethods.GetReferenceGasPrice, [], cancellationToken)
            .ConfigureAwait(false);
        return raw?.ToString() ?? "0";
    }

    /// <summary>
    /// Gets objects owned by an address, with optional pagination and filter.
    /// </summary>
    /// <param name="owner">Owner address (normalized).</param>
    /// <param name="options">Optional: cursor, limit, filter (e.g. by type or object ids).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Paginated list of object responses.</returns>
    public Task<PaginatedObjectsResponse> GetOwnedObjectsAsync(
        string owner,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        string normalized = SuiAddress.Normalize(owner.AsSpan());
        return _transport.RequestAsync<PaginatedObjectsResponse>(
            RpcMethods.GetOwnedObjects,
            [normalized, options],
            cancellationToken);
    }

    /// <summary>
    /// Gets multiple objects by IDs in one request.
    /// </summary>
    /// <param name="objectIds">Object IDs (will be normalized).</param>
    /// <param name="options">Optional request options (showOwner, showType, etc.).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Array of object responses (same order as input; missing objects may have error set).</returns>
    public Task<SuiObjectResponse[]> MultiGetObjectsAsync(
        string[] objectIds,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        if (objectIds == null || objectIds.Length == 0)
        {
            throw new ArgumentException("At least one object ID is required.", nameof(objectIds));
        }

        string[] normalized = new string[objectIds.Length];
        for (int index = 0; index < objectIds.Length; index++)
        {
            normalized[index] = SuiAddress.Normalize(objectIds[index].AsSpan());
        }

        return _transport.RequestAsync<SuiObjectResponse[]>(
            RpcMethods.MultiGetObjects,
            [normalized, options],
            cancellationToken);
    }

    /// <summary>
    /// Gets a transaction block by digest.
    /// </summary>
    /// <param name="digest">Transaction block digest.</param>
    /// <param name="options">Optional request options.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Transaction block response or null if not found.</returns>
    public Task<SuiTransactionBlockResponse?> GetTransactionBlockAsync(
        string digest,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(digest))
        {
            throw new ArgumentNullException(nameof(digest));
        }

        return _transport.RequestAsync<SuiTransactionBlockResponse?>(
            RpcMethods.GetTransactionBlock,
            [digest.Trim(), options],
            cancellationToken);
    }

    /// <summary>
    /// Gets all coin balances for an owner (all coin types).
    /// </summary>
    /// <param name="owner">Owner address.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Array of coin balances per coin type.</returns>
    public Task<CoinBalance[]> GetAllBalancesAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        string normalized = SuiAddress.Normalize(owner.AsSpan());
        return _transport.RequestAsync<CoinBalance[]>(
            RpcMethods.GetAllBalances,
            [normalized],
            cancellationToken);
    }

    /// <summary>
    /// Gets coin objects for an owner, with optional coin type and pagination.
    /// </summary>
    /// <param name="owner">Owner address.</param>
    /// <param name="coinType">Optional coin type (e.g. 0x2::sui::SUI). Null uses default SUI type.</param>
    /// <param name="cursor">Optional cursor for next page (from previous response nextCursor).</param>
    /// <param name="limit">Optional max items per page.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Paginated coin objects.</returns>
    public Task<PaginatedCoinsResponse> GetCoinsAsync(
        string owner,
        string? coinType = null,
        string? cursor = null,
        uint? limit = null,
        CancellationToken cancellationToken = default)
    {
        string normalized = SuiAddress.Normalize(owner.AsSpan());
        object?[] @params =
        [
            normalized,
            coinType,
            cursor,
            limit.HasValue ? (int)limit.Value : null
        ];
        return _transport.RequestAsync<PaginatedCoinsResponse>(
            RpcMethods.GetCoins,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Gets all coin objects for an owner (all coin types), with optional pagination (suix_getAllCoins).
    /// </summary>
    /// <param name="owner">Owner address (normalized).</param>
    /// <param name="cursor">Optional pagination cursor.</param>
    /// <param name="limit">Maximum items per page.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<PaginatedCoinsResponse> GetAllCoinsAsync(
        string owner,
        string? cursor = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentNullException(nameof(owner));
        }

        string normalized = SuiAddress.Normalize(owner.Trim().AsSpan());
        return _transport.RequestAsync<PaginatedCoinsResponse>(
            RpcMethods.GetAllCoins,
            [normalized, cursor, limit],
            cancellationToken);
    }

    /// <summary>
    /// Queries transaction blocks by filter (e.g. FromAddress, ToAddress, Checkpoint).
    /// </summary>
    /// <param name="query">Query filter (e.g. FromAddress, ToAddress, Checkpoint).</param>
    /// <param name="cursor">Optional pagination cursor.</param>
    /// <param name="limit">Optional max items per page.</param>
    /// <param name="descendingOrder">If true, newest first.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Paginated transaction block responses.</returns>
    public Task<PaginatedTransactionBlocksResponse> QueryTransactionBlocksAsync(
        object query,
        string? cursor = null,
        uint? limit = null,
        bool descendingOrder = false,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var @params = new List<object?> { query };
        if (cursor != null)
        {
            @params.Add(cursor);
        }

        if (limit.HasValue)
        {
            @params.Add((int)limit.Value);
        }

        if (descendingOrder)
        {
            @params.Add(descendingOrder);
        }

        return _transport.RequestAsync<PaginatedTransactionBlocksResponse>(
            RpcMethods.QueryTransactionBlocks,
            @params.ToArray(),
            cancellationToken);
    }

    /// <summary>
    /// Waits until a transaction block is confirmed (or timeout). Polls the node until the block is found.
    /// </summary>
    /// <param name="digest">Transaction block digest.</param>
    /// <param name="options">Optional: timeout (ms), poll_interval (ms).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Transaction block response when confirmed.</returns>
    public Task<SuiTransactionBlockResponse> WaitForTransactionBlockAsync(
        string digest,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(digest))
        {
            throw new ArgumentNullException(nameof(digest));
        }

        return _transport.RequestAsync<SuiTransactionBlockResponse>(
            RpcMethods.WaitForTransactionBlock,
            [digest.Trim(), options],
            cancellationToken);
    }

    /// <summary>
    /// Gets metadata for a coin type (decimals, name, symbol, description, icon). Returns null if not found.
    /// </summary>
    /// <param name="coinType">Fully qualified coin type (e.g. 0x2::sui::SUI).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Coin metadata or null.</returns>
    public Task<CoinMetadata?> GetCoinMetadataAsync(
        string coinType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(coinType))
        {
            throw new ArgumentNullException(nameof(coinType));
        }

        return _transport.RequestAsync<CoinMetadata?>(
            RpcMethods.GetCoinMetadata,
            [coinType.Trim()],
            cancellationToken);
    }

    /// <summary>
    /// Tries to get an object at a specific version (for past state). Returns object data or error if not found.
    /// </summary>
    /// <param name="objectId">Object ID.</param>
    /// <param name="version">Sequence number / version of the object.</param>
    /// <param name="options">Optional request options.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Object response (data or error).</returns>
    public Task<SuiObjectResponse> TryGetPastObjectAsync(
        string objectId,
        ulong version,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        string normalized = SuiAddress.Normalize(objectId.AsSpan());
        return _transport.RequestAsync<SuiObjectResponse>(
            RpcMethods.TryGetPastObject,
            [normalized, version.ToString(), options],
            cancellationToken);
    }

    /// <summary>
    /// Gets multiple transaction blocks by digests in one request.
    /// </summary>
    /// <param name="digests">Transaction block digests.</param>
    /// <param name="options">Optional request options.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Array of transaction block responses (same order as input; missing items may be null).</returns>
    public Task<SuiTransactionBlockResponse?[]> MultiGetTransactionBlocksAsync(
        string[] digests,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        if (digests == null || digests.Length == 0)
        {
            throw new ArgumentException("At least one digest is required.", nameof(digests));
        }

        return _transport.RequestAsync<SuiTransactionBlockResponse?[]>(
            RpcMethods.MultiGetTransactionBlocks,
            [digests, options],
            cancellationToken);
    }

    /// <summary>
    /// Gets total circulating supply for a coin type (value in smallest unit, e.g. MIST for SUI).
    /// </summary>
    /// <param name="coinType">Fully qualified coin type (e.g. 0x2::sui::SUI).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<TotalSupplyResponse> GetTotalSupplyAsync(
        string coinType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(coinType))
        {
            throw new ArgumentNullException(nameof(coinType));
        }

        return _transport.RequestAsync<TotalSupplyResponse>(
            RpcMethods.GetTotalSupply,
            [coinType.Trim()],
            cancellationToken);
    }

    /// <summary>
    /// Runs transaction in dev-inspect mode (no gas payment, no state change). Returns effects, results, events, gas used.
    /// </summary>
    /// <param name="sender">Sender address (can be any for simulation).</param>
    /// <param name="transactionBlockBase64">BCS-serialized transaction as base64.</param>
    /// <param name="gasPrice">Optional gas price for simulation.</param>
    /// <param name="epoch">Optional epoch to simulate in.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<DevInspectResults> DevInspectTransactionBlockAsync(
        string sender,
        string transactionBlockBase64,
        string? gasPrice = null,
        string? epoch = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sender))
        {
            throw new ArgumentNullException(nameof(sender));
        }

        if (string.IsNullOrWhiteSpace(transactionBlockBase64))
        {
            throw new ArgumentNullException(nameof(transactionBlockBase64));
        }

        string normalizedSender = SuiAddress.Normalize(sender.AsSpan());
        var @params = new List<object?> { normalizedSender, transactionBlockBase64.Trim() };
        if (gasPrice != null)
        {
            @params.Add(gasPrice);
        }

        if (epoch != null)
        {
            @params.Add(epoch);
        }

        return _transport.RequestAsync<DevInspectResults>(
            RpcMethods.DevInspectTransactionBlock,
            @params.ToArray(),
            cancellationToken);
    }

    /// <summary>
    /// Dry-runs a transaction block without submitting. Returns effects, events, object/balance changes.
    /// </summary>
    /// <param name="transactionBlockBase64">BCS-serialized transaction as base64.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<DryRunTransactionBlockResponse> DryRunTransactionBlockAsync(
        string transactionBlockBase64,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionBlockBase64))
        {
            throw new ArgumentNullException(nameof(transactionBlockBase64));
        }

        return _transport.RequestAsync<DryRunTransactionBlockResponse>(
            RpcMethods.DryRunTransactionBlock,
            [transactionBlockBase64.Trim()],
            cancellationToken);
    }

    /// <summary>
    /// Dry-runs a transaction block (bytes; encoded to base64).
    /// </summary>
    public Task<DryRunTransactionBlockResponse> DryRunTransactionBlockAsync(
        byte[] transactionBlock,
        CancellationToken cancellationToken = default)
    {
        if (transactionBlock == null || transactionBlock.Length == 0)
        {
            throw new ArgumentNullException(nameof(transactionBlock));
        }

        return DryRunTransactionBlockAsync(Base64.Encode(transactionBlock), cancellationToken);
    }

    /// <summary>
    /// Gets checkpoint by sequence number.
    /// </summary>
    /// <param name="sequenceNumber">Checkpoint sequence number.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<CheckpointResponse> GetCheckpointAsync(
        string sequenceNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sequenceNumber))
        {
            throw new ArgumentNullException(nameof(sequenceNumber));
        }

        return _transport.RequestAsync<CheckpointResponse>(
            RpcMethods.GetCheckpoint,
            [sequenceNumber.Trim()],
            cancellationToken);
    }

    /// <summary>
    /// Gets the latest checkpoint sequence number (blockchain height).
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Sequence number as string.</returns>
    public Task<string> GetLatestCheckpointSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<string>(
            RpcMethods.GetLatestCheckpointSequenceNumber,
            [],
            cancellationToken);
    }

    /// <summary>
    /// Gets chain identifier (first 4 bytes of genesis checkpoint digest; identifies mainnet/testnet/devnet).
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<string> GetChainIdentifierAsync(CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<string>(
            RpcMethods.GetChainIdentifier,
            [],
            cancellationToken);
    }

    /// <summary>
    /// Returns delegated stakes for an owner (suix_getStakes).
    /// </summary>
    /// <param name="owner">Owner address (normalized).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<DelegatedStakeResponse[]> GetStakesAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentNullException(nameof(owner));
        }

        string normalized = SuiAddress.Normalize(owner.Trim().AsSpan());
        return _transport.RequestAsync<DelegatedStakeResponse[]>(
            RpcMethods.GetStakes,
            [normalized],
            cancellationToken);
    }

    /// <summary>
    /// Returns delegated stakes by staked Sui object IDs (suix_getStakesByIds).
    /// </summary>
    /// <param name="stakedSuiIds">Staked Sui object IDs.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<DelegatedStakeResponse[]> GetStakesByIdsAsync(
        string[] stakedSuiIds,
        CancellationToken cancellationToken = default)
    {
        if (stakedSuiIds == null || stakedSuiIds.Length == 0)
        {
            throw new ArgumentException("At least one staked Sui ID is required.", nameof(stakedSuiIds));
        }

        return _transport.RequestAsync<DelegatedStakeResponse[]>(
            RpcMethods.GetStakesByIds,
            [stakedSuiIds],
            cancellationToken);
    }

    /// <summary>
    /// Returns historical checkpoints paginated (sui_getCheckpoints).
    /// </summary>
    /// <param name="cursor">Optional cursor from previous page.</param>
    /// <param name="limit">Maximum items per page.</param>
    /// <param name="descendingOrder">True for newest first.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<CheckpointPageResponse> GetCheckpointsAsync(
        string? cursor = null,
        int? limit = null,
        bool descendingOrder = true,
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<CheckpointPageResponse>(
            RpcMethods.GetCheckpoints,
            [cursor, limit, descendingOrder],
            cancellationToken);
    }

    /// <summary>
    /// Queries events by filter (suix_queryEvents).
    /// </summary>
    /// <param name="query">Event filter (e.g. All, Sender, MoveEventType, MoveModule, etc.) as JSON. Use JsonSerializer.SerializeToElement or build JsonElement.</param>
    /// <param name="cursor">Optional pagination cursor (EventId).</param>
    /// <param name="limit">Maximum events per page.</param>
    /// <param name="descendingOrder">True for newest first.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<PaginatedEventsResponse> QueryEventsAsync(
        JsonElement? query,
        object? cursor = null,
        int? limit = null,
        bool descendingOrder = true,
        CancellationToken cancellationToken = default)
    {
        object?[] @params = [query ?? JsonSerializer.SerializeToElement(new { All = Array.Empty<object>() }), cursor, limit, descendingOrder];
        return _transport.RequestAsync<PaginatedEventsResponse>(
            RpcMethods.QueryEvents,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Returns the latest Sui system state (suix_getLatestSuiSystemState).
    /// </summary>
    public Task<SuiSystemStateSummaryResponse> GetLatestSuiSystemStateAsync(
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<SuiSystemStateSummaryResponse>(
            RpcMethods.GetLatestSuiSystemState,
            [],
            cancellationToken);
    }

    /// <summary>
    /// Returns the current epoch info (suix_getCurrentEpoch).
    /// </summary>
    public Task<EpochInfoResponse> GetCurrentEpochAsync(
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<EpochInfoResponse>(
            RpcMethods.GetCurrentEpoch,
            [],
            cancellationToken);
    }

    /// <summary>
    /// Returns validators APY for the current epoch (suix_getValidatorsApy).
    /// </summary>
    public Task<ValidatorsApyResponse> GetValidatorsApyAsync(
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<ValidatorsApyResponse>(
            RpcMethods.GetValidatorsApy,
            [],
            cancellationToken);
    }

    /// <summary>
    /// Returns dynamic field objects owned by a parent object (suix_getDynamicFields).
    /// </summary>
    /// <param name="parentId">Parent object ID.</param>
    /// <param name="cursor">Optional pagination cursor.</param>
    /// <param name="limit">Maximum items per page.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<DynamicFieldPageResponse> GetDynamicFieldsAsync(
        string parentId,
        string? cursor = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(parentId))
        {
            throw new ArgumentNullException(nameof(parentId));
        }

        return _transport.RequestAsync<DynamicFieldPageResponse>(
            RpcMethods.GetDynamicFields,
            [parentId.Trim(), cursor, limit],
            cancellationToken);
    }

    /// <summary>
    /// Returns the dynamic field object for a parent and field name (suix_getDynamicFieldObject).
    /// </summary>
    /// <param name="parentId">Parent object ID.</param>
    /// <param name="name">Dynamic field name (JSON: type + value).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<SuiObjectResponse> GetDynamicFieldObjectAsync(
        string parentId,
        object name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(parentId))
        {
            throw new ArgumentNullException(nameof(parentId));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        object nameParam = name is JsonElement element ? element : JsonSerializer.SerializeToElement(name);
        return _transport.RequestAsync<SuiObjectResponse>(
            RpcMethods.GetDynamicFieldObject,
            [parentId.Trim(), nameParam],
            cancellationToken);
    }

    /// <summary>
    /// Returns committee info for an epoch (suix_getCommitteeInfo). Optional epoch defaults to latest.
    /// </summary>
    public Task<CommitteeInfoResponse> GetCommitteeInfoAsync(
        string? epoch = null,
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<CommitteeInfoResponse>(
            RpcMethods.GetCommitteeInfo,
            [epoch],
            cancellationToken);
    }

    /// <summary>
    /// Returns network metrics (suix_getNetworkMetrics): TPS, checkpoint, epoch, totals.
    /// </summary>
    public Task<NetworkMetricsResponse> GetNetworkMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<NetworkMetricsResponse>(
            RpcMethods.GetNetworkMetrics,
            [],
            cancellationToken);
    }

    /// <summary>
    /// Returns latest address metrics (suix_getLatestAddressMetrics).
    /// </summary>
    public Task<AddressMetricsResponse> GetAddressMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<AddressMetricsResponse>(
            RpcMethods.GetLatestAddressMetrics,
            [],
            cancellationToken);
    }

    /// <summary>
    /// Returns epoch metrics paginated (suix_getEpochMetrics).
    /// </summary>
    /// <param name="cursor">Optional pagination cursor.</param>
    /// <param name="limit">Maximum items per page.</param>
    /// <param name="descendingOrder">True for newest first.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<EpochMetricsPageResponse> GetEpochMetricsAsync(
        string? cursor = null,
        int? limit = null,
        bool? descendingOrder = null,
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<EpochMetricsPageResponse>(
            RpcMethods.GetEpochMetrics,
            [cursor, limit, descendingOrder],
            cancellationToken);
    }

    /// <summary>
    /// Returns protocol config (sui_getProtocolConfig). Optional version defaults to latest.
    /// </summary>
    public Task<ProtocolConfigResponse> GetProtocolConfigAsync(
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<ProtocolConfigResponse>(
            RpcMethods.GetProtocolConfig,
            [version],
            cancellationToken);
    }

    /// <summary>
    /// Returns Move call metrics ranked by 3/7/30 days (suix_getMoveCallMetrics).
    /// </summary>
    public Task<MoveCallMetricsResponse> GetMoveCallMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<MoveCallMetricsResponse>(
            RpcMethods.GetMoveCallMetrics,
            [],
            cancellationToken);
    }

    /// <summary>
    /// Returns RPC API version via rpc.discover (info.version). Returns null if not present.
    /// </summary>
    public async Task<string?> GetRpcApiVersionAsync(
        CancellationToken cancellationToken = default)
    {
        RpcDiscoverResponse response = await _transport
            .RequestAsync<RpcDiscoverResponse>(RpcMethods.RpcDiscover, [], cancellationToken)
            .ConfigureAwait(false);
        return response.Info?.Version;
    }

    /// <summary>
    /// Returns total number of transaction blocks (sui_getTotalTransactionBlocks).
    /// </summary>
    public async Task<string> GetTotalTransactionBlocksAsync(
        CancellationToken cancellationToken = default)
    {
        object? raw = await _transport
            .RequestAsync<object>(RpcMethods.GetTotalTransactionBlocks, [], cancellationToken)
            .ConfigureAwait(false);
        return raw?.ToString() ?? "0";
    }

    /// <summary>
    /// Resolves a name service name to an address (suix_resolveNameServiceAddress). Returns null if not found.
    /// </summary>
    public Task<string?> ResolveNameServiceAddressAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        return _transport.RequestAsync<string?>(
            RpcMethods.ResolveNameServiceAddress,
            [name.Trim()],
            cancellationToken);
    }

    /// <summary>
    /// Resolves an address to name service names (suix_resolveNameServiceNames), with optional pagination.
    /// </summary>
    /// <param name="address">Sui address to resolve.</param>
    /// <param name="cursor">Optional pagination cursor.</param>
    /// <param name="limit">Maximum names per page.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<ResolvedNameServiceNamesResponse> ResolveNameServiceNamesAsync(
        string address,
        string? cursor = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentNullException(nameof(address));
        }

        string normalized = SuiAddress.Normalize(address.Trim().AsSpan());
        return _transport.RequestAsync<ResolvedNameServiceNamesResponse>(
            RpcMethods.ResolveNameServiceNames,
            [normalized, cursor, limit],
            cancellationToken);
    }

    /// <summary>
    /// Returns address metrics for all epochs (suix_getAllEpochAddressMetrics).
    /// </summary>
    /// <param name="descendingOrder">True for newest first.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<AddressMetricsResponse[]> GetAllEpochAddressMetricsAsync(
        bool? descendingOrder = null,
        CancellationToken cancellationToken = default)
    {
        return _transport.RequestAsync<AddressMetricsResponse[]>(
            RpcMethods.GetAllEpochAddressMetrics,
            [descendingOrder],
            cancellationToken);
    }

    /// <summary>
    /// Returns Move function argument types (sui_getMoveFunctionArgTypes). Result is a JSON array of 'Pure' or { Object: ... }.
    /// </summary>
    /// <param name="packageId">Package ID (e.g. 0x2).</param>
    /// <param name="module">Module name.</param>
    /// <param name="function">Function name.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>JSON array of argument types (use .EnumerateArray() to iterate).</returns>
    public Task<JsonElement> GetMoveFunctionArgTypesAsync(
        string packageId,
        string module,
        string function,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentNullException(nameof(packageId));
        }

        if (string.IsNullOrWhiteSpace(module))
        {
            throw new ArgumentNullException(nameof(module));
        }

        if (string.IsNullOrWhiteSpace(function))
        {
            throw new ArgumentNullException(nameof(function));
        }

        return _transport.RequestAsync<JsonElement>(
            RpcMethods.GetMoveFunctionArgTypes,
            [packageId.Trim(), module.Trim(), function.Trim()],
            cancellationToken);
    }

    /// <summary>
    /// Returns normalized Move modules by package (sui_getNormalizedMoveModulesByPackage). Result is a JSON object (module name → SuiMoveNormalizedModule).
    /// </summary>
    public Task<JsonElement> GetNormalizedMoveModulesByPackageAsync(
        string packageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentNullException(nameof(packageId));
        }

        return _transport.RequestAsync<JsonElement>(
            RpcMethods.GetNormalizedMoveModulesByPackage,
            [packageId.Trim()],
            cancellationToken);
    }

    /// <summary>
    /// Returns a single normalized Move module (sui_getNormalizedMoveModule).
    /// </summary>
    public Task<JsonElement> GetNormalizedMoveModuleAsync(
        string packageId,
        string module,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentNullException(nameof(packageId));
        }

        if (string.IsNullOrWhiteSpace(module))
        {
            throw new ArgumentNullException(nameof(module));
        }

        return _transport.RequestAsync<JsonElement>(
            RpcMethods.GetNormalizedMoveModule,
            [packageId.Trim(), module.Trim()],
            cancellationToken);
    }

    /// <summary>
    /// Returns normalized Move function (sui_getNormalizedMoveFunction): parameters, return, typeParameters, visibility, isEntry.
    /// </summary>
    public Task<JsonElement> GetNormalizedMoveFunctionAsync(
        string packageId,
        string module,
        string function,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentNullException(nameof(packageId));
        }

        if (string.IsNullOrWhiteSpace(module))
        {
            throw new ArgumentNullException(nameof(module));
        }

        if (string.IsNullOrWhiteSpace(function))
        {
            throw new ArgumentNullException(nameof(function));
        }

        return _transport.RequestAsync<JsonElement>(
            RpcMethods.GetNormalizedMoveFunction,
            [packageId.Trim(), module.Trim(), function.Trim()],
            cancellationToken);
    }

    /// <summary>
    /// Returns normalized Move struct (sui_getNormalizedMoveStruct): abilities, fields, typeParameters.
    /// </summary>
    public Task<JsonElement> GetNormalizedMoveStructAsync(
        string packageId,
        string module,
        string structName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentNullException(nameof(packageId));
        }

        if (string.IsNullOrWhiteSpace(module))
        {
            throw new ArgumentNullException(nameof(module));
        }

        if (string.IsNullOrWhiteSpace(structName))
        {
            throw new ArgumentNullException(nameof(structName));
        }

        return _transport.RequestAsync<JsonElement>(
            RpcMethods.GetNormalizedMoveStruct,
            [packageId.Trim(), module.Trim(), structName.Trim()],
            cancellationToken);
    }
}
