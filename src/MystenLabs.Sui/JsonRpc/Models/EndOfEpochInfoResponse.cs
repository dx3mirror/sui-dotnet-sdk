namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// End-of-epoch summary (last checkpoint, timestamps, protocol version, stake/gas totals).
/// </summary>
public sealed class EndOfEpochInfoResponse
{
    [JsonPropertyName("lastCheckpointId")]
    public string? LastCheckpointId { get; set; }

    [JsonPropertyName("epochEndTimestamp")]
    public string? EpochEndTimestamp { get; set; }

    [JsonPropertyName("protocolVersion")]
    public string? ProtocolVersion { get; set; }

    [JsonPropertyName("referenceGasPrice")]
    public string? ReferenceGasPrice { get; set; }

    [JsonPropertyName("totalStake")]
    public string? TotalStake { get; set; }

    [JsonPropertyName("storageFundBalance")]
    public string? StorageFundBalance { get; set; }

    [JsonPropertyName("totalGasFees")]
    public string? TotalGasFees { get; set; }

    [JsonPropertyName("totalStakeRewardsDistributed")]
    public string? TotalStakeRewardsDistributed { get; set; }
}
