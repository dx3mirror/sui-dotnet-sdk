namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Event cursor for pagination (txDigest + eventSeq).
/// </summary>
public sealed class EventIdResponse
{
    [JsonPropertyName("txDigest")]
    public string? TxDigest { get; set; }

    [JsonPropertyName("eventSeq")]
    public string? EventSeq { get; set; }
}

/// <summary>
/// Single event from suix_queryEvents (id, packageId, type, sender, parsedJson, etc.).
/// </summary>
public sealed class SuiEventResponse
{
    [JsonPropertyName("id")]
    public EventIdResponse? Id { get; set; }

    [JsonPropertyName("packageId")]
    public string? PackageId { get; set; }

    [JsonPropertyName("transactionModule")]
    public string? TransactionModule { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    [JsonPropertyName("timestampMs")]
    public string? TimestampMs { get; set; }

    [JsonPropertyName("parsedJson")]
    public System.Text.Json.JsonElement? ParsedJson { get; set; }

    [JsonPropertyName("bcs")]
    public string? Bcs { get; set; }

    [JsonPropertyName("bcsEncoding")]
    public string? BcsEncoding { get; set; }
}

/// <summary>
/// Paginated events from suix_queryEvents (data, hasNextPage, nextCursor).
/// </summary>
public sealed class PaginatedEventsResponse
{
    [JsonPropertyName("data")]
    public SuiEventResponse[]? Data { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("nextCursor")]
    public EventIdResponse? NextCursor { get; set; }
}
