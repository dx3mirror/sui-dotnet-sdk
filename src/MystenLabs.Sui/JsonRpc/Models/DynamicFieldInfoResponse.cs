namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Single dynamic field entry (name, objectId, type, version, bcsName, etc.).
/// </summary>
public sealed class DynamicFieldInfoResponse
{
    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("name")]
    public JsonElement? Name { get; set; }

    [JsonPropertyName("objectId")]
    public string? ObjectId { get; set; }

    [JsonPropertyName("objectType")]
    public string? ObjectType { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("bcsEncoding")]
    public string? BcsEncoding { get; set; }

    [JsonPropertyName("bcsName")]
    public string? BcsName { get; set; }
}
