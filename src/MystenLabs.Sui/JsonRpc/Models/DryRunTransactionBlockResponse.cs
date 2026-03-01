namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Result of sui_dryRunTransactionBlock: transaction effects and execution details without submitting.
/// </summary>
public sealed class DryRunTransactionBlockResponse
{
    [JsonPropertyName("effects")]
    public SuiTransactionBlockEffects? Effects { get; set; }

    [JsonPropertyName("events")]
    public object? Events { get; set; }

    [JsonPropertyName("objectChanges")]
    public object? ObjectChanges { get; set; }

    [JsonPropertyName("balanceChanges")]
    public object? BalanceChanges { get; set; }
}
