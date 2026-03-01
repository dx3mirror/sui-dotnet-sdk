namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Paginated checkpoints from sui_getCheckpoints (data, nextCursor, hasNextPage).
/// </summary>
public sealed class CheckpointPageResponse
{
    [JsonPropertyName("data")]
    public CheckpointResponse[]? Data { get; set; }

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}
