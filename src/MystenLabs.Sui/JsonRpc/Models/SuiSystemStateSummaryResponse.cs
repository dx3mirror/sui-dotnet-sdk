namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// JSON-RPC type for the Sui system state (suix_getLatestSuiSystemState). Flattened top-level fields.
/// </summary>
public sealed class SuiSystemStateSummaryResponse
{
    [JsonPropertyName("activeValidators")]
    public SuiValidatorSummaryResponse[]? ActiveValidators { get; set; }

    [JsonPropertyName("epoch")]
    public string? Epoch { get; set; }

    [JsonPropertyName("epochDurationMs")]
    public string? EpochDurationMs { get; set; }

    [JsonPropertyName("epochStartTimestampMs")]
    public string? EpochStartTimestampMs { get; set; }

    [JsonPropertyName("maxValidatorCount")]
    public string? MaxValidatorCount { get; set; }

    [JsonPropertyName("minValidatorJoiningStake")]
    public string? MinValidatorJoiningStake { get; set; }

    [JsonPropertyName("protocolVersion")]
    public string? ProtocolVersion { get; set; }

    [JsonPropertyName("referenceGasPrice")]
    public string? ReferenceGasPrice { get; set; }

    [JsonPropertyName("safeMode")]
    public bool SafeMode { get; set; }

    [JsonPropertyName("stakeSubsidyBalance")]
    public string? StakeSubsidyBalance { get; set; }

    [JsonPropertyName("stakeSubsidyCurrentDistributionAmount")]
    public string? StakeSubsidyCurrentDistributionAmount { get; set; }

    [JsonPropertyName("stakeSubsidyDecreaseRate")]
    public int StakeSubsidyDecreaseRate { get; set; }

    [JsonPropertyName("stakeSubsidyDistributionCounter")]
    public string? StakeSubsidyDistributionCounter { get; set; }

    [JsonPropertyName("stakeSubsidyPeriodLength")]
    public string? StakeSubsidyPeriodLength { get; set; }

    [JsonPropertyName("stakeSubsidyStartEpoch")]
    public string? StakeSubsidyStartEpoch { get; set; }

    [JsonPropertyName("storageFundNonRefundableBalance")]
    public string? StorageFundNonRefundableBalance { get; set; }

    [JsonPropertyName("storageFundTotalObjectStorageRebates")]
    public string? StorageFundTotalObjectStorageRebates { get; set; }

    [JsonPropertyName("systemStateVersion")]
    public string? SystemStateVersion { get; set; }

    [JsonPropertyName("totalStake")]
    public string? TotalStake { get; set; }

    [JsonPropertyName("validatorLowStakeGracePeriod")]
    public string? ValidatorLowStakeGracePeriod { get; set; }

    [JsonPropertyName("validatorLowStakeThreshold")]
    public string? ValidatorLowStakeThreshold { get; set; }

    [JsonPropertyName("validatorVeryLowStakeThreshold")]
    public string? ValidatorVeryLowStakeThreshold { get; set; }
}
