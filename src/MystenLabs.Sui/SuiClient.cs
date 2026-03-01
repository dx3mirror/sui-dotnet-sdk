namespace MystenLabs.Sui;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Faucet;
using MystenLabs.Sui.JsonRpc;
using MystenLabs.Sui.JsonRpc.Models;
using MystenLabs.Sui.Transactions;

/// <summary>
/// High-level Sui client: RPC (get object, balance, execute transaction) and signing helpers.
/// </summary>
public sealed class SuiClient
{
    private const char UrlPathSeparator = '/';

    private readonly SuiRpcClient _rpc;

    /// <summary>
    /// Creates a client for the given Sui RPC endpoint.
    /// </summary>
    /// <param name="rpcUrl">Base URL of the Sui RPC node (e.g. https://fullnode.mainnet.sui.io).</param>
    /// <param name="httpClient">Optional HTTP client; if null, a default instance is used.</param>
    public SuiClient(string rpcUrl, HttpClient? httpClient = null)
    {
        string baseUrl = rpcUrl?.TrimEnd(UrlPathSeparator) ?? throw new ArgumentNullException(nameof(rpcUrl));
        _rpc = new SuiRpcClient(baseUrl, httpClient ?? new HttpClient());
    }

    /// <summary>
    /// Creates a client using the given RPC client (e.g. for testing).
    /// </summary>
    public SuiClient(SuiRpcClient rpcClient)
    {
        _rpc = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
    }

    /// <summary>
    /// Gets an object by ID. Returns the RPC response (data or error).
    /// </summary>
    /// <param name="objectId">Object ID (0x + 64 hex or normalized).</param>
    /// <param name="options">Optional request options.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<SuiObjectResponse> GetObjectAsync(
        string objectId,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetObjectAsync(objectId, options, cancellationToken);
    }

    /// <summary>
    /// Gets the coin balance for an owner and optional coin type.
    /// </summary>
    /// <param name="owner">Owner address.</param>
    /// <param name="coinType">Optional coin type (defaults to SUI).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<CoinBalance> GetBalanceAsync(
        string owner,
        string? coinType = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetBalanceAsync(owner, coinType, cancellationToken);
    }

    /// <summary>
    /// Gets the reference gas price (for building transactions).
    /// </summary>
    public Task<string> GetReferenceGasPriceAsync(CancellationToken cancellationToken = default)
    {
        return _rpc.GetReferenceGasPriceAsync(cancellationToken);
    }

    /// <summary>
    /// Gets objects owned by an address (with optional pagination and filter).
    /// </summary>
    public Task<PaginatedObjectsResponse> GetOwnedObjectsAsync(
        string owner,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetOwnedObjectsAsync(owner, options, cancellationToken);
    }

    /// <summary>
    /// Gets multiple objects by IDs in one request.
    /// </summary>
    public Task<SuiObjectResponse[]> MultiGetObjectsAsync(
        string[] objectIds,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.MultiGetObjectsAsync(objectIds, options, cancellationToken);
    }

    /// <summary>
    /// Gets a transaction block by digest.
    /// </summary>
    public Task<SuiTransactionBlockResponse?> GetTransactionBlockAsync(
        string digest,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetTransactionBlockAsync(digest, options, cancellationToken);
    }

    /// <summary>
    /// Gets all coin balances for an owner (all coin types).
    /// </summary>
    public Task<CoinBalance[]> GetAllBalancesAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetAllBalancesAsync(owner, cancellationToken);
    }

    /// <summary>
    /// Gets coin objects for an owner (optional coin type and pagination).
    /// </summary>
    public Task<PaginatedCoinsResponse> GetCoinsAsync(
        string owner,
        string? coinType = null,
        string? cursor = null,
        uint? limit = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetCoinsAsync(owner, coinType, cursor, limit, cancellationToken);
    }

    /// <summary>
    /// Gets all coin objects for an owner (all coin types), with optional pagination.
    /// </summary>
    public Task<PaginatedCoinsResponse> GetAllCoinsAsync(
        string owner,
        string? cursor = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetAllCoinsAsync(owner, cursor, limit, cancellationToken);
    }

    /// <summary>
    /// Queries transaction blocks by filter (FromAddress, ToAddress, Checkpoint, etc.).
    /// </summary>
    public Task<PaginatedTransactionBlocksResponse> QueryTransactionBlocksAsync(
        object query,
        string? cursor = null,
        uint? limit = null,
        bool descendingOrder = false,
        CancellationToken cancellationToken = default)
    {
        return _rpc.QueryTransactionBlocksAsync(query, cursor, limit, descendingOrder, cancellationToken);
    }

    /// <summary>
    /// Waits until a transaction block is confirmed (or timeout).
    /// </summary>
    public Task<SuiTransactionBlockResponse> WaitForTransactionBlockAsync(
        string digest,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.WaitForTransactionBlockAsync(digest, options, cancellationToken);
    }

    /// <summary>
    /// Executes a signed transaction block (transaction and signatures as base64).
    /// </summary>
    public Task<SuiTransactionBlockResponse> ExecuteTransactionBlockAsync(
        string transactionBlockBase64,
        string[] signatures,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.ExecuteTransactionBlockAsync(transactionBlockBase64, signatures, options, cancellationToken);
    }

    /// <summary>
    /// Signs the transaction with the signer and executes it.
    /// </summary>
    /// <param name="transaction">Built transaction (from <see cref="TransactionDataBuilder.Build"/> wrapped in <see cref="Transaction"/>).</param>
    /// <param name="signer">Signer (e.g. Ed25519Keypair).</param>
    /// <param name="options">Optional execute options.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Transaction block response (digest, effects, etc.).</returns>
    public Task<SuiTransactionBlockResponse> SignAndExecuteTransactionBlockAsync(
        Transaction transaction,
        Signer signer,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        if (transaction == null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        (byte[] serialized, string signature) = transaction.Sign(signer);
        return _rpc.ExecuteTransactionBlockAsync(
            MystenLabs.Sui.Utils.Base64.Encode(serialized),
            new[] { signature },
            options,
            cancellationToken);
    }

    /// <summary>
    /// Signs the given serialized transaction bytes with the signer (TransactionData intent) and executes the transaction.
    /// </summary>
    /// <param name="serializedTransaction">BCS-serialized transaction data (V1 or V2).</param>
    /// <param name="signer">Signer (e.g. Ed25519Keypair).</param>
    /// <param name="options">Optional execute options.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Transaction block response (digest, effects, etc.).</returns>
    public async Task<SuiTransactionBlockResponse> SignAndExecuteTransactionBlockAsync(
        byte[] serializedTransaction,
        Signer signer,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        if (serializedTransaction == null || serializedTransaction.Length == 0)
        {
            throw new ArgumentNullException(nameof(serializedTransaction));
        }

        if (signer == null)
        {
            throw new ArgumentNullException(nameof(signer));
        }

        byte[] digest = TransactionHasher.GetDigestToSign(serializedTransaction);
        byte[] signature = signer.Sign(digest);
        string serializedSignature = Signature.ToSerializedSignature(signer.GetKeyScheme(), signature, signer.GetPublicKey());

        return await _rpc
            .ExecuteTransactionBlockAsync(
                MystenLabs.Sui.Utils.Base64.Encode(serializedTransaction),
                new[] { serializedSignature },
                options,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets metadata for a coin type (decimals, name, symbol, description). Returns null if not found.
    /// </summary>
    public Task<CoinMetadata?> GetCoinMetadataAsync(
        string coinType,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetCoinMetadataAsync(coinType, cancellationToken);
    }

    /// <summary>
    /// Tries to get an object at a specific version (past state).
    /// </summary>
    public Task<SuiObjectResponse> TryGetPastObjectAsync(
        string objectId,
        ulong version,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.TryGetPastObjectAsync(objectId, version, options, cancellationToken);
    }

    /// <summary>
    /// Gets multiple transaction blocks by digests in one request.
    /// </summary>
    public Task<SuiTransactionBlockResponse?[]> MultiGetTransactionBlocksAsync(
        string[] digests,
        object? options = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.MultiGetTransactionBlocksAsync(digests, options, cancellationToken);
    }

    /// <summary>
    /// Gets total circulating supply for a coin type (value in smallest unit).
    /// </summary>
    public Task<TotalSupplyResponse> GetTotalSupplyAsync(
        string coinType,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetTotalSupplyAsync(coinType, cancellationToken);
    }

    /// <summary>
    /// Runs transaction in dev-inspect mode (simulation without gas or state change).
    /// </summary>
    public Task<DevInspectResults> DevInspectTransactionBlockAsync(
        string sender,
        string transactionBlockBase64,
        string? gasPrice = null,
        string? epoch = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.DevInspectTransactionBlockAsync(sender, transactionBlockBase64, gasPrice, epoch, cancellationToken);
    }

    /// <summary>
    /// Dry-runs a transaction block without submitting.
    /// </summary>
    public Task<DryRunTransactionBlockResponse> DryRunTransactionBlockAsync(
        string transactionBlockBase64,
        CancellationToken cancellationToken = default)
    {
        return _rpc.DryRunTransactionBlockAsync(transactionBlockBase64, cancellationToken);
    }

    /// <summary>
    /// Dry-runs a transaction block (bytes).
    /// </summary>
    public Task<DryRunTransactionBlockResponse> DryRunTransactionBlockAsync(
        byte[] transactionBlock,
        CancellationToken cancellationToken = default)
    {
        return _rpc.DryRunTransactionBlockAsync(transactionBlock, cancellationToken);
    }

    /// <summary>
    /// Gets checkpoint by sequence number.
    /// </summary>
    public Task<CheckpointResponse> GetCheckpointAsync(
        string sequenceNumber,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetCheckpointAsync(sequenceNumber, cancellationToken);
    }

    /// <summary>
    /// Gets the latest checkpoint sequence number.
    /// </summary>
    public Task<string> GetLatestCheckpointSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        return _rpc.GetLatestCheckpointSequenceNumberAsync(cancellationToken);
    }

    /// <summary>
    /// Gets chain identifier (mainnet/testnet/devnet).
    /// </summary>
    public Task<string> GetChainIdentifierAsync(CancellationToken cancellationToken = default)
    {
        return _rpc.GetChainIdentifierAsync(cancellationToken);
    }

    /// <summary>
    /// Gets delegated stakes for an owner address.
    /// </summary>
    public Task<DelegatedStakeResponse[]> GetStakesAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetStakesAsync(owner, cancellationToken);
    }

    /// <summary>
    /// Gets delegated stakes by staked SUI object IDs.
    /// </summary>
    public Task<DelegatedStakeResponse[]> GetStakesByIdsAsync(
        string[] stakedSuiIds,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetStakesByIdsAsync(stakedSuiIds, cancellationToken);
    }

    /// <summary>
    /// Gets checkpoints with optional pagination.
    /// </summary>
    public Task<CheckpointPageResponse> GetCheckpointsAsync(
        string? cursor = null,
        int? limit = null,
        bool descendingOrder = false,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetCheckpointsAsync(cursor, limit, descendingOrder, cancellationToken);
    }

    /// <summary>
    /// Queries events by filter (optional), with pagination.
    /// </summary>
    /// <param name="query">Optional event filter as JSON (e.g. EventType, Sender); null means all events.</param>
    /// <param name="cursor">Optional pagination cursor from previous response.</param>
    /// <param name="limit">Maximum events per page.</param>
    /// <param name="descendingOrder">True for newest first.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<PaginatedEventsResponse> QueryEventsAsync(
        System.Text.Json.JsonElement? query = null,
        string? cursor = null,
        int? limit = null,
        bool descendingOrder = false,
        CancellationToken cancellationToken = default)
    {
        return _rpc.QueryEventsAsync(query, cursor, limit, descendingOrder, cancellationToken);
    }

    /// <summary>
    /// Gets the latest Sui system state (validators, epoch, gas, stake subsidy, etc.).
    /// </summary>
    public Task<SuiSystemStateSummaryResponse> GetLatestSuiSystemStateAsync(
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetLatestSuiSystemStateAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the current epoch info (validators, checkpoint range, timestamps).
    /// </summary>
    public Task<EpochInfoResponse> GetCurrentEpochAsync(
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetCurrentEpochAsync(cancellationToken);
    }

    /// <summary>
    /// Gets validators APY for the current epoch.
    /// </summary>
    public Task<ValidatorsApyResponse> GetValidatorsApyAsync(
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetValidatorsApyAsync(cancellationToken);
    }

    /// <summary>
    /// Gets dynamic field objects owned by a parent object (paginated).
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
        return _rpc.GetDynamicFieldsAsync(parentId, cursor, limit, cancellationToken);
    }

    /// <summary>
    /// Gets the dynamic field object for a parent and field name.
    /// </summary>
    /// <param name="parentId">Parent object ID.</param>
    /// <param name="name">Dynamic field name (object with type and value, or JsonElement).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<SuiObjectResponse> GetDynamicFieldObjectAsync(
        string parentId,
        object name,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetDynamicFieldObjectAsync(parentId, name, cancellationToken);
    }

    /// <summary>
    /// Gets committee info for an epoch (optional; null = latest epoch).
    /// </summary>
    public Task<CommitteeInfoResponse> GetCommitteeInfoAsync(
        string? epoch = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetCommitteeInfoAsync(epoch, cancellationToken);
    }

    /// <summary>
    /// Gets network metrics (TPS, checkpoint, epoch, total addresses/objects/packages).
    /// </summary>
    public Task<NetworkMetricsResponse> GetNetworkMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetNetworkMetricsAsync(cancellationToken);
    }

    /// <summary>
    /// Gets latest address metrics (cumulative and daily active addresses).
    /// </summary>
    public Task<AddressMetricsResponse> GetAddressMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetAddressMetricsAsync(cancellationToken);
    }

    /// <summary>
    /// Gets epoch metrics with optional pagination.
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
        return _rpc.GetEpochMetricsAsync(cursor, limit, descendingOrder, cancellationToken);
    }

    /// <summary>
    /// Gets protocol config (optional version; null = latest).
    /// </summary>
    public Task<ProtocolConfigResponse> GetProtocolConfigAsync(
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetProtocolConfigAsync(version, cancellationToken);
    }

    /// <summary>
    /// Gets Move call metrics (ranked by 3/7/30 days).
    /// </summary>
    public Task<MoveCallMetricsResponse> GetMoveCallMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetMoveCallMetricsAsync(cancellationToken);
    }

    /// <summary>
    /// Gets RPC API version (from rpc.discover). Returns null if not available.
    /// </summary>
    public Task<string?> GetRpcApiVersionAsync(
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetRpcApiVersionAsync(cancellationToken);
    }

    /// <summary>
    /// Gets total number of transaction blocks.
    /// </summary>
    public Task<string> GetTotalTransactionBlocksAsync(
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetTotalTransactionBlocksAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves a name service name to an address. Returns null if not found.
    /// </summary>
    public Task<string?> ResolveNameServiceAddressAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return _rpc.ResolveNameServiceAddressAsync(name, cancellationToken);
    }

    /// <summary>
    /// Resolves an address to name service names, with optional pagination.
    /// </summary>
    public Task<ResolvedNameServiceNamesResponse> ResolveNameServiceNamesAsync(
        string address,
        string? cursor = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.ResolveNameServiceNamesAsync(address, cursor, limit, cancellationToken);
    }

    /// <summary>
    /// Gets address metrics for all epochs (optional descending order).
    /// </summary>
    public Task<AddressMetricsResponse[]> GetAllEpochAddressMetricsAsync(
        bool? descendingOrder = null,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetAllEpochAddressMetricsAsync(descendingOrder, cancellationToken);
    }

    /// <summary>
    /// Gets Move function argument types for a package::module::function (returns JSON array).
    /// </summary>
    public Task<System.Text.Json.JsonElement> GetMoveFunctionArgTypesAsync(
        string packageId,
        string module,
        string function,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetMoveFunctionArgTypesAsync(packageId, module, function, cancellationToken);
    }

    /// <summary>
    /// Gets normalized Move modules by package (returns JSON object: module name → normalized module).
    /// </summary>
    public Task<System.Text.Json.JsonElement> GetNormalizedMoveModulesByPackageAsync(
        string packageId,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetNormalizedMoveModulesByPackageAsync(packageId, cancellationToken);
    }

    /// <summary>
    /// Gets a single normalized Move module.
    /// </summary>
    public Task<System.Text.Json.JsonElement> GetNormalizedMoveModuleAsync(
        string packageId,
        string module,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetNormalizedMoveModuleAsync(packageId, module, cancellationToken);
    }

    /// <summary>
    /// Gets normalized Move function (parameters, return, visibility, isEntry, etc.).
    /// </summary>
    public Task<System.Text.Json.JsonElement> GetNormalizedMoveFunctionAsync(
        string packageId,
        string module,
        string function,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetNormalizedMoveFunctionAsync(packageId, module, function, cancellationToken);
    }

    /// <summary>
    /// Gets normalized Move struct (abilities, fields, typeParameters).
    /// </summary>
    public Task<System.Text.Json.JsonElement> GetNormalizedMoveStructAsync(
        string packageId,
        string module,
        string structName,
        CancellationToken cancellationToken = default)
    {
        return _rpc.GetNormalizedMoveStructAsync(packageId, module, structName, cancellationToken);
    }

    /// <summary>
    /// Requests gas/SUI from the faucet for the given network (testnet, devnet, or localnet).
    /// </summary>
    /// <param name="network">One of: testnet, devnet, localnet.</param>
    /// <param name="recipient">Sui address to receive the coins (normalized 0x + 64 hex).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Result containing the list of coins sent.</returns>
    /// <exception cref="FaucetRateLimitException">Faucet returned HTTP 429 (rate limit).</exception>
    public Task<FaucetRequestSuiResult> RequestSuiFromFaucetAsync(
        string network,
        string recipient,
        CancellationToken cancellationToken = default)
    {
        string host = FaucetClient.GetFaucetHost(network);
        return FaucetClient.RequestSuiFromFaucetV2Async(host, recipient, null, cancellationToken);
    }
}
