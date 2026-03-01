namespace MystenLabs.Sui.JsonRpc.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Response from sui_getObject: either object data or an error payload.
/// </summary>
public sealed class SuiObjectResponse
{
    [JsonPropertyName("data")]
    public SuiObjectData? Data { get; set; }

    [JsonPropertyName("error")]
    public SuiObjectResponseError? Error { get; set; }
}

/// <summary>
/// Object data as returned by the RPC (subset of fields).
/// </summary>
public sealed class SuiObjectData
{
    [JsonPropertyName("objectId")]
    public string? ObjectId { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("owner")]
    public object? Owner { get; set; }

    [JsonPropertyName("content")]
    public object? Content { get; set; }
}

/// <summary>
/// Error payload when getObject fails (e.g. object not found).
/// </summary>
public sealed class SuiObjectResponseError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
