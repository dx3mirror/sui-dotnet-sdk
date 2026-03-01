namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Move call metrics (suix_getMoveCallMetrics): ranked by 3/7/30 days.
/// Each rank array contains entries as JSON arrays [ { module, package, function }, valueString ].
/// </summary>
public sealed class MoveCallMetricsResponse
{
    [JsonPropertyName("rank3Days")]
    public JsonElement[]? Rank3Days { get; set; }

    [JsonPropertyName("rank7Days")]
    public JsonElement[]? Rank7Days { get; set; }

    [JsonPropertyName("rank30Days")]
    public JsonElement[]? Rank30Days { get; set; }
}
