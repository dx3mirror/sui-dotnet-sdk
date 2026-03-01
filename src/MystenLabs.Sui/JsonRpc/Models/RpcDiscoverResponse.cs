namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Response shape for rpc.discover (OpenRPC); used to read API version.
/// </summary>
public sealed class RpcDiscoverResponse
{
    [JsonPropertyName("info")]
    public RpcDiscoverInfo? Info { get; set; }
}

/// <summary>
/// Info block of rpc.discover response.
/// </summary>
public sealed class RpcDiscoverInfo
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}
