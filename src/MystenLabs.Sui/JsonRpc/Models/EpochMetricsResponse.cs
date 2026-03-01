namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Single epoch metrics entry (epoch, transaction count, checkpoint range, timestamps).
/// </summary>
public sealed class EpochMetricsResponse
{
    [JsonPropertyName("epoch")]
    public string? Epoch { get; set; }

    [JsonPropertyName("epochTotalTransactions")]
    public string? EpochTotalTransactions { get; set; }

    [JsonPropertyName("firstCheckpointId")]
    public string? FirstCheckpointId { get; set; }

    [JsonPropertyName("epochStartTimestamp")]
    public string? EpochStartTimestamp { get; set; }

    [JsonPropertyName("endOfEpochInfo")]
    public EndOfEpochInfoResponse? EndOfEpochInfo { get; set; }
}
