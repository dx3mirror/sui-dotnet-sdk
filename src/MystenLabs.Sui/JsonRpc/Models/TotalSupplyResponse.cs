namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Total supply of a coin type from suix_getTotalSupply (value in smallest unit, e.g. MIST for SUI).
/// </summary>
public sealed class TotalSupplyResponse
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
