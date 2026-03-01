namespace MystenLabs.Sui.Tests.Utils;

using MystenLabs.Sui.Utils;
using Xunit;

public sealed class HexTests
{
    [Fact]
    public void Encode_Decode_RoundTrip()
    {
        byte[] bytes = { 0x1a, 0x2b, 0x3c };
        string hex = Hex.Encode(bytes);
        Assert.Equal("1a2b3c", hex);
        byte[] decoded = Hex.Decode(hex);
        Assert.Equal(bytes, decoded);
    }

    [Fact]
    public void Decode_Accepts_0x_Prefix()
    {
        byte[] expected = { 0x0a, 0x2b };
        byte[] decoded = Hex.Decode("0x0a2b");
        Assert.Equal(expected, decoded);
    }

    [Fact]
    public void Decode_OddLength_Pads_Leading_Zero()
    {
        byte[] decoded = Hex.Decode("a2b");
        Assert.Equal(2, decoded.Length);
        Assert.Equal(0x0a, decoded[0]);
        Assert.Equal(0x2b, decoded[1]);
        decoded = Hex.Decode("0a2b");
        Assert.Equal(2, decoded.Length);
        Assert.Equal(0x0a, decoded[0]);
        Assert.Equal(0x2b, decoded[1]);
    }

    [Fact]
    public void Encode_Empty_Returns_Empty()
    {
        Assert.Equal("", Hex.Encode([]));
    }

    [Fact]
    public void Decode_Empty_Returns_Empty()
    {
        Assert.Empty(Hex.Decode(""));
        Assert.Empty(Hex.Decode("0x"));
    }

    [Fact]
    public void IsHexChar_Accepts_Valid_Digits()
    {
        Assert.True(Hex.IsHexChar('0'));
        Assert.True(Hex.IsHexChar('9'));
        Assert.True(Hex.IsHexChar('a'));
        Assert.True(Hex.IsHexChar('f'));
        Assert.True(Hex.IsHexChar('A'));
        Assert.True(Hex.IsHexChar('F'));
    }

    [Fact]
    public void IsHexChar_Rejects_NonHex()
    {
        Assert.False(Hex.IsHexChar('g'));
        Assert.False(Hex.IsHexChar('z'));
        Assert.False(Hex.IsHexChar(' '));
        Assert.False(Hex.IsHexChar('x'));
    }
}
