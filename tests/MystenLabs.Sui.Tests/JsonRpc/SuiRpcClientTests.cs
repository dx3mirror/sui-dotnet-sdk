namespace MystenLabs.Sui.Tests.JsonRpc;

using System.Net;
using System.Text;
using System.Text.Json;
using MystenLabs.Sui.JsonRpc;
using MystenLabs.Sui.JsonRpc.Models;
using Xunit;

public sealed class SuiRpcClientTests
{
    /// <summary>
    /// Returns an HttpClient that always responds with the given JSON-RPC result body (result object only; wrapped in jsonrpc envelope).
    /// </summary>
    private static HttpClient CreateMockHttpClient(object result)
    {
        var envelope = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["result"] = result,
            ["error"] = null
        };
        string json = JsonSerializer.Serialize(envelope);
        var handler = new MockResponseHandler(json);
        return new HttpClient(handler);
    }

    [Fact]
    public async Task GetCoinsAsync_Deserializes_Response()
    {
        var result = new
        {
            data = new[]
            {
                new
                {
                    coinType = "0x2::sui::SUI",
                    coinObjectId = "0xobj1",
                    version = "1",
                    digest = "d1",
                    balance = "5000"
                }
            },
            nextCursor = (string?)"cursor",
            hasNextPage = true
        };
        using HttpClient client = CreateMockHttpClient(result);
        var rpc = new SuiRpcClient("http://localhost", client);
        PaginatedCoinsResponse response = await rpc.GetCoinsAsync("0x0000000000000000000000000000000000000001");
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal("0x2::sui::SUI", response.Data[0].CoinType);
        Assert.Equal("5000", response.Data[0].Balance);
        Assert.True(response.HasNextPage);
    }

    [Fact]
    public async Task QueryTransactionBlocksAsync_Deserializes_Response()
    {
        var result = new
        {
            data = new[]
            {
                new { digest = "txDigest1", effects = (object?)null }
            },
            nextCursor = (string?)"cursor",
            hasNextPage = false
        };
        using HttpClient client = CreateMockHttpClient(result);
        var rpc = new SuiRpcClient("http://localhost", client);
        PaginatedTransactionBlocksResponse response = await rpc.QueryTransactionBlocksAsync(
            new { FromAddress = "0x0000000000000000000000000000000000000001" });
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal("txDigest1", response.Data[0].Digest);
        Assert.False(response.HasNextPage);
    }

    [Fact]
    public async Task WaitForTransactionBlockAsync_Deserializes_Response()
    {
        var result = new
        {
            digest = "waitDigest",
            effects = (object?)null
        };
        using HttpClient client = CreateMockHttpClient(result);
        var rpc = new SuiRpcClient("http://localhost", client);
        SuiTransactionBlockResponse response = await rpc.WaitForTransactionBlockAsync("waitDigest");
        Assert.NotNull(response);
        Assert.Equal("waitDigest", response.Digest);
    }

    private sealed class MockResponseHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        internal MockResponseHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
