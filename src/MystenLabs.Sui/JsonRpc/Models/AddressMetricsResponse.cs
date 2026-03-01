namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Address metrics (suix_getLatestAddressMetrics): checkpoint, epoch, cumulative and daily active addresses.
/// </summary>
public sealed class AddressMetricsResponse
{
    [JsonPropertyName("checkpoint")]
    public long Checkpoint { get; set; }

    [JsonPropertyName("epoch")]
    public long Epoch { get; set; }

    [JsonPropertyName("timestampMs")]
    public long TimestampMs { get; set; }

    [JsonPropertyName("cumulativeAddresses")]
    public long CumulativeAddresses { get; set; }

    [JsonPropertyName("cumulativeActiveAddresses")]
    public long CumulativeActiveAddresses { get; set; }

    [JsonPropertyName("dailyActiveAddresses")]
    public long DailyActiveAddresses { get; set; }
}
