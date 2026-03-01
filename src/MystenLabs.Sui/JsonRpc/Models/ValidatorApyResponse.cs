namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Validator APY entry (address and annual percentage yield).
/// </summary>
public sealed class ValidatorApyResponse
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("apy")]
    public double Apy { get; set; }
}
