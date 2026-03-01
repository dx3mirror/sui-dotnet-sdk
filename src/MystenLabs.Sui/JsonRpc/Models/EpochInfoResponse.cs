namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Current epoch info (suix_getCurrentEpoch): epoch, validators, checkpoint range, timestamps.
/// </summary>
public sealed class EpochInfoResponse
{
    [JsonPropertyName("epoch")]
    public string? Epoch { get; set; }

    [JsonPropertyName("validators")]
    public SuiValidatorSummaryResponse[]? Validators { get; set; }

    [JsonPropertyName("epochTotalTransactions")]
    public string? EpochTotalTransactions { get; set; }

    [JsonPropertyName("firstCheckpointId")]
    public string? FirstCheckpointId { get; set; }

    [JsonPropertyName("epochStartTimestamp")]
    public string? EpochStartTimestamp { get; set; }

    [JsonPropertyName("endOfEpochInfo")]
    public EndOfEpochInfoResponse? EndOfEpochInfo { get; set; }

    [JsonPropertyName("referenceGasPrice")]
    public long? ReferenceGasPrice { get; set; }
}
