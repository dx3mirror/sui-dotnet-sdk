namespace MystenLabs.Sui.Tests.Utils;

using MystenLabs.Sui.Utils;
using Xunit;

public sealed class Base58Tests
{
    [Fact]
    public void Encode_Decode_RoundTrip()
    {
        byte[] bytes = { 1, 2, 3 };
        string encoded = Base58.Encode(bytes);
        byte[] decoded = Base58.Decode(encoded);
        Assert.Equal(bytes, decoded);
    }

    [Fact]
    public void Encode_Empty_Returns_Empty()
    {
        Assert.Equal("", Base58.Encode([]));
    }

    [Fact]
    public void Decode_Empty_Returns_Empty()
    {
        Assert.Empty(Base58.Decode(""));
    }
}
