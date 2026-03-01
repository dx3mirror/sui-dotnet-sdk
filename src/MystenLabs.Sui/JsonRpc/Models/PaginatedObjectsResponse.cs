namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Paginated response from suix_getOwnedObjects (data, next cursor, hasNextPage).
/// </summary>
public sealed class PaginatedObjectsResponse
{
    /// <summary>
    /// Page of object responses.
    /// </summary>
    [JsonPropertyName("data")]
    public SuiObjectResponse[]? Data { get; set; }

    /// <summary>
    /// Cursor for the next page; null if no more pages.
    /// </summary>
    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    /// <summary>
    /// Whether another page of results exists.
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}
