namespace MystenLabs.Sui.Tests.JsonRpc;

using System.Text.Json;
using MystenLabs.Sui.JsonRpc.Models;
using Xunit;

public sealed class PaginatedCoinsResponseTests
{
    [Fact]
    public void Deserialize_From_Valid_Json_Populates_Data_And_Cursor()
    {
        string json = """
            {
                "data": [
                    {
                        "coinType": "0x2::sui::SUI",
                        "coinObjectId": "0xabc123",
                        "version": "1",
                        "digest": "digest1",
                        "balance": "1000000"
                    }
                ],
                "nextCursor": "cursor1",
                "hasNextPage": true
            }
            """;
        PaginatedCoinsResponse? response = JsonSerializer.Deserialize<PaginatedCoinsResponse>(json);
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal("0x2::sui::SUI", response.Data[0].CoinType);
        Assert.Equal("0xabc123", response.Data[0].CoinObjectId);
        Assert.Equal("1000000", response.Data[0].Balance);
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
        PaginatedCoinsResponse? response = JsonSerializer.Deserialize<PaginatedCoinsResponse>(json);
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
        Assert.False(response.HasNextPage);
    }
}
