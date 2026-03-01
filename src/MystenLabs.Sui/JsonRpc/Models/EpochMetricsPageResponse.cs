namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Paginated epoch metrics (suix_getEpochMetrics): data, nextCursor, hasNextPage.
/// </summary>
public sealed class EpochMetricsPageResponse
{
    [JsonPropertyName("data")]
    public EpochMetricsResponse[]? Data { get; set; }

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}
