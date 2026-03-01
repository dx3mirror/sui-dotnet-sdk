namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Single stake entry within a delegation (principal, epochs, status; optional estimatedReward when Active).
/// </summary>
public sealed class StakeObjectResponse
{
    [JsonPropertyName("principal")]
    public string? Principal { get; set; }

    [JsonPropertyName("stakeActiveEpoch")]
    public string? StakeActiveEpoch { get; set; }

    [JsonPropertyName("stakeRequestEpoch")]
    public string? StakeRequestEpoch { get; set; }

    [JsonPropertyName("stakedSuiId")]
    public string? StakedSuiId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("estimatedReward")]
    public string? EstimatedReward { get; set; }
}

/// <summary>
/// Delegated stake for an owner: staking pool, validator, and list of stake objects (suix_getStakes / suix_getStakesByIds).
/// </summary>
public sealed class DelegatedStakeResponse
{
    [JsonPropertyName("stakes")]
    public StakeObjectResponse[]? Stakes { get; set; }

    [JsonPropertyName("stakingPool")]
    public string? StakingPool { get; set; }

    [JsonPropertyName("validatorAddress")]
    public string? ValidatorAddress { get; set; }
}
