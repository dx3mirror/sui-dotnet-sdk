namespace MystenLabs.Sui.Tests;

using System.Net;
using System.Text;
using System.Text.Json;
using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.JsonRpc;
using MystenLabs.Sui.JsonRpc.Models;
using MystenLabs.Sui.Keypairs.Ed25519;
using MystenLabs.Sui;
using MystenLabs.Sui.Transactions;
using Xunit;

public sealed class SuiClientTests
{
    private const string Sender = "0x0000000000000000000000000000000000000001";

    [Fact]
    public async Task SignAndExecuteTransactionBlockAsync_Transaction_Overload_Returns_Mocked_Response()
    {
        var executeResult = new
        {
            digest = "mockDigest123",
            effects = (object?)null
        };
        var envelope = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["result"] = executeResult,
            ["error"] = null
        };
        string json = JsonSerializer.Serialize(envelope);
        var handler = new MockHttpHandler(json);
        using var httpClient = new HttpClient(handler);
        var rpc = new SuiRpcClient("http://localhost", httpClient);
        var client = new SuiClient(rpc);

        GasData gasData = new GasData(
            [],
            Sender,
            Price: 1,
            Budget: 1000);
        TransactionData data = new TransactionDataBuilder()
            .SetSender(Sender)
            .SetGasData(gasData)
            .SetExpirationNone()
            .SetInputs([])
            .SetCommands(
            [
                new CommandMoveCall(new ProgrammableMoveCall(
                    "0x2", "sui", "transfer",
                    [],
                    []))
            ])
            .Build();
        var transaction = new Transaction(data);
        Ed25519Keypair keypair = Ed25519Keypair.Generate();

        SuiTransactionBlockResponse response = await client.SignAndExecuteTransactionBlockAsync(transaction, keypair);

        Assert.NotNull(response);
        Assert.Equal("mockDigest123", response.Digest);
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        internal MockHttpHandler(string responseJson)
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
