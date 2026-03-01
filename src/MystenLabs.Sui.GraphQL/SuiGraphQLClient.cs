namespace MystenLabs.Sui.GraphQL;

using System.Text.Json;
using global::GraphQL;
using global::GraphQL.Client.Http;
using global::GraphQL.Client.Serializer.SystemTextJson;

/// <summary>
/// Sui GraphQL client. Connects to the Sui GraphQL RPC (general-purpose indexer) for typed queries.
/// Beta endpoints: https://graphql.mainnet.sui.io/graphql, https://graphql.testnet.sui.io/graphql.
/// </summary>
public sealed class SuiGraphQLClient : IDisposable
{
    /// <summary>
    /// Default mainnet GraphQL endpoint.
    /// </summary>
    public const string DefaultMainnetEndpoint = "https://graphql.mainnet.sui.io/graphql";

    /// <summary>
    /// Default testnet GraphQL endpoint.
    /// </summary>
    public const string DefaultTestnetEndpoint = "https://graphql.testnet.sui.io/graphql";

    private readonly GraphQLHttpClient _client;

    /// <summary>
    /// Creates a GraphQL client for the given endpoint.
    /// </summary>
    /// <param name="endpoint">GraphQL endpoint URL (e.g. https://graphql.mainnet.sui.io/graphql).</param>
    /// <param name="httpClient">Optional HTTP client; if null, a default one is used.</param>
    public SuiGraphQLClient(string endpoint, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        string url = endpoint.TrimEnd('/');
        if (!url.EndsWith("/graphql", StringComparison.OrdinalIgnoreCase))
        {
            url = url + "/graphql";
        }

        var options = new GraphQLHttpClientOptions { EndPoint = new Uri(url) };
        _client = httpClient != null
            ? new GraphQLHttpClient(options, new SystemTextJsonSerializer(), httpClient)
            : new GraphQLHttpClient(options, new SystemTextJsonSerializer());
    }

    /// <summary>
    /// Executes a GraphQL query (or mutation) and returns the result as a typed response.
    /// </summary>
    /// <param name="query">GraphQL query/mutation string.</param>
    /// <param name="variables">Optional variables (JSON-serializable object).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>GraphQL response with Data and/or Errors.</returns>
    public Task<GraphQLResponse<JsonElement>> ExecuteAsync(
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentNullException(nameof(query));
        }

        var request = new GraphQLRequest(query, variables);
        return _client.SendQueryAsync<JsonElement>(request, cancellationToken);
    }

    /// <summary>
    /// Executes a GraphQL query and returns the raw response (Data, Errors, Extensions).
    /// </summary>
    /// <param name="request">GraphQL request (query + variables).</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public Task<GraphQLResponse<JsonElement>> ExecuteAsync(
        GraphQLRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return _client.SendQueryAsync<JsonElement>(request, cancellationToken);
    }

    /// <summary>
    /// Disposes the underlying HTTP client (if owned by this client).
    /// </summary>
    public void Dispose() => _client.Dispose();
}
