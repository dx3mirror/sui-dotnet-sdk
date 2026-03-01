namespace MystenLabs.Sui.Faucet;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Sui faucet host URLs and helpers for requesting gas/SUI on testnet, devnet, or localnet.
/// </summary>
public static class FaucetClient
{
    private const string ContentTypeJson = "application/json";
    private const int HttpTooManyRequests = 429;
    private const string PathV2Gas = "/v2/gas";
    private const string StatusSuccess = "Success";
    private const string PropertyFailure = "Failure";
    private const string PropertyInternal = "internal";

    /// <summary>
    /// Testnet faucet base URL.
    /// </summary>
    public const string HostTestnet = "https://faucet.testnet.sui.io";

    /// <summary>
    /// Devnet faucet base URL.
    /// </summary>
    public const string HostDevnet = "https://faucet.devnet.sui.io";

    /// <summary>
    /// Localnet faucet base URL (default port 9123).
    /// </summary>
    public const string HostLocalnet = "http://127.0.0.1:9123";

    /// <summary>
    /// Returns the faucet base URL for the given network. Faucets exist only for testnet, devnet, and localnet.
    /// </summary>
    /// <param name="network">One of: testnet, devnet, localnet.</param>
    /// <returns>Base URL of the faucet (e.g. https://faucet.devnet.sui.io).</returns>
    public static string GetFaucetHost(string network)
    {
        return network?.ToLowerInvariant() switch
        {
            "testnet" => HostTestnet,
            "devnet" => HostDevnet,
            "localnet" => HostLocalnet,
            _ => throw new ArgumentException($"Unknown network: {network}. Faucet is available for testnet, devnet, localnet only.", nameof(network))
        };
    }

    /// <summary>
    /// Requests gas/SUI from the faucet for the given recipient (v2 API). Uses the default HttpClient.
    /// </summary>
    /// <param name="host">Faucet base URL (e.g. from <see cref="GetFaucetHost"/>).</param>
    /// <param name="recipient">Sui address to receive the coins (normalized 0x + 64 hex).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Result containing the list of coins sent.</returns>
    /// <exception cref="FaucetRateLimitException">Faucet returned HTTP 429 (rate limit).</exception>
    public static Task<FaucetRequestSuiResult> RequestSuiFromFaucetV2Async(
        string host,
        string recipient,
        CancellationToken cancellationToken = default)
    {
        return RequestSuiFromFaucetV2Async(host, recipient, null, cancellationToken);
    }

    /// <summary>
    /// Requests gas/SUI from the faucet for the given recipient (v2 API).
    /// </summary>
    /// <param name="host">Faucet base URL (e.g. from <see cref="GetFaucetHost"/>).</param>
    /// <param name="recipient">Sui address to receive the coins (normalized 0x + 64 hex).</param>
    /// <param name="httpClient">HTTP client to use; if null, a default instance is used.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>Result containing the list of coins sent.</returns>
    /// <exception cref="FaucetRateLimitException">Faucet returned HTTP 429 (rate limit).</exception>
    public static async Task<FaucetRequestSuiResult> RequestSuiFromFaucetV2Async(
        string host,
        string recipient,
        HttpClient? httpClient,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentNullException(nameof(host));
        }

        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentNullException(nameof(recipient));
        }

        string baseUrl = host.TrimEnd('/');
        string url = baseUrl + PathV2Gas;
        HttpClient client = httpClient ?? new HttpClient();

        var body = new FixedAmountRequestPayload
        {
            FixedAmountRequest = new FixedAmountRequest
            {
                Recipient = recipient.Trim()
            }
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            ContentTypeJson);

        using var response = await client.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == (System.Net.HttpStatusCode)HttpTooManyRequests)
        {
            throw new FaucetRateLimitException();
        }

        string rawJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        FaucetResponseV2Dto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<FaucetResponseV2Dto>(rawJson);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Encountered error when parsing response from faucet. Status: {response.StatusCode}, response: {rawJson}.",
                exception);
        }

        if (dto == null)
        {
            throw new InvalidOperationException(
                $"Empty or invalid faucet response. Status: {response.StatusCode}, response: {rawJson}.");
        }

        if (dto.Status.ValueKind == JsonValueKind.String &&
            dto.Status.GetString() == StatusSuccess)
        {
            IReadOnlyList<FaucetCoinInfo> coins = dto.CoinsSent ?? [];
            return new FaucetRequestSuiResult(coins);
        }

        string failureMessage = TryGetFailureMessage(dto.Status) ?? "Unknown failure.";
        throw new InvalidOperationException($"Faucet request failed: {failureMessage}");
    }

    private static string? TryGetFailureMessage(JsonElement status)
    {
        if (status.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!status.TryGetProperty(PropertyFailure, out JsonElement failure))
        {
            return null;
        }

        if (failure.TryGetProperty(PropertyInternal, out JsonElement internalElement))
        {
            return internalElement.GetString();
        }

        return failure.GetRawText();
    }

    private sealed class FixedAmountRequestPayload
    {
        [JsonPropertyName("FixedAmountRequest")]
        public FixedAmountRequest? FixedAmountRequest { get; set; }
    }

    private sealed class FixedAmountRequest
    {
        [JsonPropertyName("recipient")]
        public string? Recipient { get; set; }
    }

    private sealed class FaucetResponseV2Dto
    {
        [JsonPropertyName("status")]
        public JsonElement Status { get; set; }

        [JsonPropertyName("coins_sent")]
        public List<FaucetCoinInfo>? CoinsSent { get; set; }
    }
}
