namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Balance response from suix_getBalance.
/// </summary>
public sealed class CoinBalance
{
    [JsonPropertyName("coinType")]
    public string? CoinType { get; set; }

    [JsonPropertyName("totalBalance")]
    public string? TotalBalance { get; set; }

    [JsonPropertyName("fundsInAddressBalance")]
    public string? FundsInAddressBalance { get; set; }
}
