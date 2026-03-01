namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Paginated response from suix_queryTransactionBlocks (data, nextCursor, hasNextPage).
/// </summary>
public sealed class PaginatedTransactionBlocksResponse
{
    [JsonPropertyName("data")]
    public SuiTransactionBlockResponse[]? Data { get; set; }

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}
