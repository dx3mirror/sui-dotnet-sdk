namespace MystenLabs.Sui.Tests.Utils;

using MystenLabs.Sui.Utils;
using Xunit;

public sealed class Base64Tests
{
    [Fact]
    public void Encode_Decode_RoundTrip()
    {
        byte[] bytes = { 0x01, 0x02, 0x03 };
        string encoded = Base64.Encode(bytes);
        byte[] decoded = Base64.Decode(encoded);
        Assert.Equal(bytes, decoded);
    }

    [Fact]
    public void Encode_Empty_Returns_Empty()
    {
        Assert.Equal("", Base64.Encode([]));
    }

    [Fact]
    public void Decode_Empty_Returns_Empty()
    {
        Assert.Empty(Base64.Decode(""));
    }

    [Fact]
    public void Decode_StandardBase64_Returns_Bytes()
    {
        byte[] expected = { 0x61, 0x62, 0x63 };
        byte[] decoded = Base64.Decode("YWJj");
        Assert.Equal(expected, decoded);
    }
}
