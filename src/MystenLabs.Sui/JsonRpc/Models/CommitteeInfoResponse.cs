namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Committee info (suix_getCommitteeInfo): epoch and validators (public key base64, stake pairs).
/// </summary>
public sealed class CommitteeInfoResponse
{
    [JsonPropertyName("epoch")]
    public string? Epoch { get; set; }

    /// <summary>
    /// Validator (public key bytes base64, stake amount) pairs.
    /// </summary>
    [JsonPropertyName("validators")]
    public string[][]? Validators { get; set; }
}
