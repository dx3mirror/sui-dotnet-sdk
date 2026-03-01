namespace MystenLabs.Sui.Tests.JsonRpc;

using System.Text.Json;
using MystenLabs.Sui.JsonRpc.Models;
using Xunit;

public sealed class PaginatedTransactionBlocksResponseTests
{
    [Fact]
    public void Deserialize_From_Valid_Json_Populates_Data_And_Cursor()
    {
        string json = """
            {
                "data": [
                    {
                        "digest": "txDigest1",
                        "effects": {}
                    }
                ],
                "nextCursor": "cursor1",
                "hasNextPage": true
            }
            """;
        PaginatedTransactionBlocksResponse? response =
            JsonSerializer.Deserialize<PaginatedTransactionBlocksResponse>(json);
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal("txDigest1", response.Data[0].Digest);
        Assert.Equal("cursor1", response.NextCursor);
        Assert.True(response.HasNextPage);
    }

    [Fact]
    public void Deserialize_Empty_Data_And_No_NextPage()
    {
        string json = """
            {
                "data": [],
                "nextCursor": null,
                "hasNextPage": false
            }
            """;
        PaginatedTransactionBlocksResponse? response =
            JsonSerializer.Deserialize<PaginatedTransactionBlocksResponse>(json);
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
        Assert.False(response.HasNextPage);
    }
}
