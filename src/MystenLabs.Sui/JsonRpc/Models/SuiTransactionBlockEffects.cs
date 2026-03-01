namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Object reference as returned in transaction effects (objectId, version, digest).
/// </summary>
public sealed class SuiObjectRefEffect
{
    [JsonPropertyName("objectId")]
    public string? ObjectId { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("digest")]
    public string? Digest { get; set; }
}

/// <summary>
/// Created or mutated entry: RPC returns { "reference": { objectId, version, digest } }.
/// </summary>
public sealed class SuiEffectRefEntry
{
    [JsonPropertyName("reference")]
    public SuiObjectRefEffect? Reference { get; set; }
}

/// <summary>
/// Transaction block effects (created, mutated, deleted objects). Matches the "effects" object
/// returned by sui_executeTransactionBlock, sui_getTransactionBlock, sui_dryRunTransactionBlock.
/// </summary>
public sealed class SuiTransactionBlockEffects
{
    [JsonPropertyName("status")]
    public object? Status { get; set; }

    /// <summary>
    /// Objects created by the transaction. Each has reference with objectId, version, digest.
    /// </summary>
    [JsonPropertyName("created")]
    public SuiEffectRefEntry[]? Created { get; set; }

    /// <summary>
    /// Objects mutated by the transaction. Each has reference with objectId, version, digest.
    /// </summary>
    [JsonPropertyName("mutated")]
    public SuiEffectRefEntry[]? Mutated { get; set; }

    /// <summary>
    /// Objects deleted by the transaction. Each may have reference or just objectId.
    /// </summary>
    [JsonPropertyName("deleted")]
    public SuiObjectRefEffect[]? Deleted { get; set; }
}
