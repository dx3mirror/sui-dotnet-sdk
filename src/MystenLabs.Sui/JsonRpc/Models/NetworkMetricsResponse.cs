namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Network metrics (suix_getNetworkMetrics): TPS, checkpoint, epoch, totals.
/// </summary>
public sealed class NetworkMetricsResponse
{
    [JsonPropertyName("currentTps")]
    public double CurrentTps { get; set; }

    [JsonPropertyName("tps30Days")]
    public double Tps30Days { get; set; }

    [JsonPropertyName("currentCheckpoint")]
    public string? CurrentCheckpoint { get; set; }

    [JsonPropertyName("currentEpoch")]
    public string? CurrentEpoch { get; set; }

    [JsonPropertyName("totalAddresses")]
    public string? TotalAddresses { get; set; }

    [JsonPropertyName("totalObjects")]
    public string? TotalObjects { get; set; }

    [JsonPropertyName("totalPackages")]
    public string? TotalPackages { get; set; }
}
