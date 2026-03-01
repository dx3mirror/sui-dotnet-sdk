namespace MystenLabs.Sui.JsonRpc;

/// <summary>
/// Sui JSON-RPC method names (no magic strings in client code).
/// </summary>
public static class RpcMethods
{
    public const string GetObject = "sui_getObject";
    public const string GetBalance = "suix_getBalance";
    public const string GetAllBalances = "suix_getAllBalances";
    public const string GetOwnedObjects = "suix_getOwnedObjects";
    public const string ExecuteTransactionBlock = "sui_executeTransactionBlock";
    public const string GetTransactionBlock = "sui_getTransactionBlock";
    public const string GetReferenceGasPrice = "suix_getReferenceGasPrice";
    public const string MultiGetObjects = "sui_multiGetObjects";

    public const string GetCoins = "suix_getCoins";

    public const string QueryTransactionBlocks = "suix_queryTransactionBlocks";

    public const string WaitForTransactionBlock = "sui_waitForTransactionBlock";

    public const string GetCoinMetadata = "suix_getCoinMetadata";

    public const string TryGetPastObject = "sui_tryGetPastObject";

    public const string MultiGetTransactionBlocks = "sui_multiGetTransactionBlocks";

    public const string GetTotalSupply = "suix_getTotalSupply";

    public const string DevInspectTransactionBlock = "sui_devInspectTransactionBlock";

    public const string DryRunTransactionBlock = "sui_dryRunTransactionBlock";

    public const string GetCheckpoint = "sui_getCheckpoint";

    public const string GetLatestCheckpointSequenceNumber = "sui_getLatestCheckpointSequenceNumber";

    public const string GetChainIdentifier = "sui_getChainIdentifier";

    public const string GetStakes = "suix_getStakes";

    public const string GetStakesByIds = "suix_getStakesByIds";

    public const string GetCheckpoints = "sui_getCheckpoints";

    public const string QueryEvents = "suix_queryEvents";

    public const string GetLatestSuiSystemState = "suix_getLatestSuiSystemState";

    public const string GetCurrentEpoch = "suix_getCurrentEpoch";

    public const string GetValidatorsApy = "suix_getValidatorsApy";

    public const string GetDynamicFields = "suix_getDynamicFields";

    public const string GetDynamicFieldObject = "suix_getDynamicFieldObject";

    public const string GetCommitteeInfo = "suix_getCommitteeInfo";

    public const string GetNetworkMetrics = "suix_getNetworkMetrics";

    public const string GetLatestAddressMetrics = "suix_getLatestAddressMetrics";

    public const string GetEpochMetrics = "suix_getEpochMetrics";

    public const string GetProtocolConfig = "sui_getProtocolConfig";

    public const string GetMoveCallMetrics = "suix_getMoveCallMetrics";

    /// <summary>
    /// OpenRPC discover (returns API version from info.version).
    /// </summary>
    public const string RpcDiscover = "rpc.discover";

    public const string GetAllCoins = "suix_getAllCoins";

    public const string GetTotalTransactionBlocks = "sui_getTotalTransactionBlocks";

    public const string ResolveNameServiceAddress = "suix_resolveNameServiceAddress";

    public const string ResolveNameServiceNames = "suix_resolveNameServiceNames";

    public const string GetAllEpochAddressMetrics = "suix_getAllEpochAddressMetrics";

    public const string GetMoveFunctionArgTypes = "sui_getMoveFunctionArgTypes";

    public const string GetNormalizedMoveModulesByPackage = "sui_getNormalizedMoveModulesByPackage";

    public const string GetNormalizedMoveModule = "sui_getNormalizedMoveModule";

    public const string GetNormalizedMoveFunction = "sui_getNormalizedMoveFunction";

    public const string GetNormalizedMoveStruct = "sui_getNormalizedMoveStruct";
}
