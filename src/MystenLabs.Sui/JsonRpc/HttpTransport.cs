namespace MystenLabs.Sui.JsonRpc;

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// HTTP JSON-RPC transport for Sui RPC (POST with JSON body, parse result or error).
/// </summary>
public sealed class HttpTransport
{
    private const string JsonRpcVersion = "2.0";
    private const string ContentTypeJson = "application/json";

    private readonly HttpClient _httpClient;
    private readonly string _url;
    private int _requestId;

    /// <summary>
    /// Creates a transport that POSTs to the given URL using the provided or default HttpClient.
    /// </summary>
    /// <param name="url">Base RPC URL (e.g. https://fullnode.devnet.sui.io:443).</param>
    /// <param name="httpClient">Optional; if null, a new HttpClient is used.</param>
    public HttpTransport(string url, HttpClient? httpClient = null)
    {
        _url = url?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(url));
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// Sends a single JSON-RPC request and returns the result, or throws on error/HTTP failure.
    /// </summary>
    /// <param name="method">RPC method name (e.g. sui_getObject).</param>
    /// <param name="params">Method parameters (will be serialized as JSON array).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <typeparam name="T">Expected result type (deserialized from result).</typeparam>
    /// <returns>Deserialized result.</returns>
    public async Task<T> RequestAsync<T>(
        string method,
        object?[]? @params,
        CancellationToken cancellationToken = default)
    {
        int requestId = Interlocked.Increment(ref _requestId);
        var requestBody = new JsonRpcRequest
        {
            Jsonrpc = JsonRpcVersion,
            Id = requestId,
            Method = method,
            Params = @params ?? []
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            ContentTypeJson);

        using var response = await _httpClient
            .PostAsync(_url, content, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new SuiHttpStatusException(
                $"Unexpected status code: {response.StatusCode}",
                (int)response.StatusCode,
                response.ReasonPhrase ?? string.Empty);
        }

        JsonRpcResponse<T>? rpcResponse = await response.Content.ReadFromJsonAsync<JsonRpcResponse<T>>(cancellationToken).ConfigureAwait(false);
        if (rpcResponse == null)
        {
            throw new SuiRpcException("Empty response from RPC.");
        }

        if (rpcResponse.Error != null)
        {
            throw new SuiJsonRpcException(
                rpcResponse.Error.Message ?? "Unknown RPC error",
                rpcResponse.Error.Code);
        }

        return rpcResponse.Result!;
    }

    private sealed class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = "";

        [JsonPropertyName("params")]
        public object?[] Params { get; set; } = [];
    }

    private sealed class JsonRpcResponse<T>
    {
        [JsonPropertyName("result")]
        public T? Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcErrorPayload? Error { get; set; }
    }

    private sealed class JsonRpcErrorPayload
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
