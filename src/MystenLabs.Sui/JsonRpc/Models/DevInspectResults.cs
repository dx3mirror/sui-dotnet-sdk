namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Result of sui_devInspectTransactionBlock: effects, return values, events, gas used (no state change).
/// </summary>
public sealed class DevInspectResults
{
    [JsonPropertyName("effects")]
    public SuiTransactionBlockEffects? Effects { get; set; }

    [JsonPropertyName("results")]
    public object? Results { get; set; }

    [JsonPropertyName("events")]
    public object? Events { get; set; }

    [JsonPropertyName("gasUsed")]
    public object? GasUsed { get; set; }
}
