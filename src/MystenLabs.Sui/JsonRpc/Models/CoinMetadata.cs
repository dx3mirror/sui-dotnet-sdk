namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Coin metadata from suix_getCoinMetadata (decimals, name, symbol, description, icon).
/// </summary>
public sealed class CoinMetadata
{
    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
