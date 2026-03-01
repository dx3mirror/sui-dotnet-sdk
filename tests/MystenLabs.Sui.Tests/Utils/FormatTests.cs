namespace MystenLabs.Sui.Tests.Utils;

using MystenLabs.Sui.Utils;
using Xunit;

public sealed class FormatTests
{
    [Fact]
    public void FormatAddress_Short_ReturnsAsIs()
    {
        Assert.Equal("0x1", Format.FormatAddress("0x1"));
        Assert.Equal("abc", Format.FormatAddress("abc"));
    }

    [Fact]
    public void FormatAddress_Long_ReturnsShortened()
    {
        string address = "0x" + new string('a', 64);
        string result = Format.FormatAddress(address);
        Assert.StartsWith("0x", result);
        Assert.Contains("\u2026", result);
        Assert.EndsWith("aaaa", result);
    }

    [Fact]
    public void FormatDigest_Short_ReturnsAsIs()
    {
        Assert.Equal("abc", Format.FormatDigest("abc"));
    }

    [Fact]
    public void FormatDigest_Long_ReturnsFirst10PlusEllipsis()
    {
        string digest = new string('x', 20);
        string result = Format.FormatDigest(digest);
        Assert.Equal(10 + 1, result.Length); // 10 chars + ellipsis (1 char)
        Assert.StartsWith("xxxxxxxxxx", result);
        Assert.EndsWith("\u2026", result);
    }
}
