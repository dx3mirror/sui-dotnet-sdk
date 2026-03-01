namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Paginated dynamic fields (suix_getDynamicFields): data, nextCursor, hasNextPage.
/// </summary>
public sealed class DynamicFieldPageResponse
{
    [JsonPropertyName("data")]
    public DynamicFieldInfoResponse[]? Data { get; set; }

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}
