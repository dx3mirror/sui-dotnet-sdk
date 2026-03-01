namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Single coin entry from suix_getCoins (coin object id, type, balance).
/// </summary>
public sealed class SuiCoinObject
{
    [JsonPropertyName("coinType")]
    public string? CoinType { get; set; }

    [JsonPropertyName("coinObjectId")]
    public string? CoinObjectId { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("balance")]
    public string? Balance { get; set; }
}

/// <summary>
/// Paginated response from suix_getCoins (data, nextCursor, hasNextPage).
/// </summary>
public sealed class PaginatedCoinsResponse
{
    [JsonPropertyName("data")]
    public SuiCoinObject[]? Data { get; set; }

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}
