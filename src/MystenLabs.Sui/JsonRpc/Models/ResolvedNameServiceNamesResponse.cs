namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Resolved name service names (suix_resolveNameServiceNames): data (names), nextCursor, hasNextPage.
/// </summary>
public sealed class ResolvedNameServiceNamesResponse
{
    [JsonPropertyName("data")]
    public string[]? Data { get; set; }

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}
