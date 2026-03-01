namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Checkpoint data from sui_getCheckpoint (sequence number, digest, timestamp, etc.).
/// </summary>
public sealed class CheckpointResponse
{
    [JsonPropertyName("epoch")]
    public string? Epoch { get; set; }

    [JsonPropertyName("sequenceNumber")]
    public string? SequenceNumber { get; set; }

    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("timestampMs")]
    public string? TimestampMs { get; set; }

    [JsonPropertyName("transactions")]
    public string[]? Transactions { get; set; }
}
