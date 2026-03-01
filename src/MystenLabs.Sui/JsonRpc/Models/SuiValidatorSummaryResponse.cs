namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// JSON-RPC representation of a Sui validator (flattened top-level fields).
/// </summary>
public sealed class SuiValidatorSummaryResponse
{
    [JsonPropertyName("commissionRate")]
    public string? CommissionRate { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("exchangeRatesId")]
    public string? ExchangeRatesId { get; set; }

    [JsonPropertyName("exchangeRatesSize")]
    public string? ExchangeRatesSize { get; set; }

    [JsonPropertyName("gasPrice")]
    public string? GasPrice { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("netAddress")]
    public string? NetAddress { get; set; }

    [JsonPropertyName("networkPubkeyBytes")]
    public string? NetworkPubkeyBytes { get; set; }

    [JsonPropertyName("nextEpochCommissionRate")]
    public string? NextEpochCommissionRate { get; set; }

    [JsonPropertyName("nextEpochGasPrice")]
    public string? NextEpochGasPrice { get; set; }

    [JsonPropertyName("nextEpochStake")]
    public string? NextEpochStake { get; set; }

    [JsonPropertyName("operationCapId")]
    public string? OperationCapId { get; set; }

    [JsonPropertyName("p2pAddress")]
    public string? P2pAddress { get; set; }

    [JsonPropertyName("pendingPoolTokenWithdraw")]
    public string? PendingPoolTokenWithdraw { get; set; }

    [JsonPropertyName("pendingStake")]
    public string? PendingStake { get; set; }

    [JsonPropertyName("pendingTotalSuiWithdraw")]
    public string? PendingTotalSuiWithdraw { get; set; }

    [JsonPropertyName("poolTokenBalance")]
    public string? PoolTokenBalance { get; set; }

    [JsonPropertyName("primaryAddress")]
    public string? PrimaryAddress { get; set; }

    [JsonPropertyName("projectUrl")]
    public string? ProjectUrl { get; set; }

    [JsonPropertyName("proofOfPossessionBytes")]
    public string? ProofOfPossessionBytes { get; set; }

    [JsonPropertyName("protocolPubkeyBytes")]
    public string? ProtocolPubkeyBytes { get; set; }

    [JsonPropertyName("rewardsPool")]
    public string? RewardsPool { get; set; }

    [JsonPropertyName("stakingPoolId")]
    public string? StakingPoolId { get; set; }

    [JsonPropertyName("stakingPoolSuiBalance")]
    public string? StakingPoolSuiBalance { get; set; }

    [JsonPropertyName("suiAddress")]
    public string? SuiAddress { get; set; }

    [JsonPropertyName("votingPower")]
    public string? VotingPower { get; set; }

    [JsonPropertyName("workerAddress")]
    public string? WorkerAddress { get; set; }
}
