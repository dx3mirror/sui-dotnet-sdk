namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Response from sui_executeTransactionBlock.
/// </summary>
public sealed class SuiTransactionBlockResponse
{
    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("effects")]
    public SuiTransactionBlockEffects? Effects { get; set; }

    [JsonPropertyName("rawEffects")]
    public byte[]? RawEffects { get; set; }
}
