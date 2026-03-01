namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Protocol config (sui_getProtocolConfig): attributes, feature flags, supported version range.
/// </summary>
public sealed class ProtocolConfigResponse
{
    [JsonPropertyName("attributes")]
    public System.Collections.Generic.Dictionary<string, JsonElement>? Attributes { get; set; }

    [JsonPropertyName("featureFlags")]
    public System.Collections.Generic.Dictionary<string, bool>? FeatureFlags { get; set; }

    [JsonPropertyName("maxSupportedProtocolVersion")]
    public string? MaxSupportedProtocolVersion { get; set; }

    [JsonPropertyName("minSupportedProtocolVersion")]
    public string? MinSupportedProtocolVersion { get; set; }

    [JsonPropertyName("protocolVersion")]
    public string? ProtocolVersion { get; set; }
}
