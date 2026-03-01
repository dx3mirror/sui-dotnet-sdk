namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Validators APY response (suix_getValidatorsApy): apys and epoch.
/// </summary>
public sealed class ValidatorsApyResponse
{
    [JsonPropertyName("apys")]
    public ValidatorApyResponse[]? Apys { get; set; }

    [JsonPropertyName("epoch")]
    public string? Epoch { get; set; }
}
