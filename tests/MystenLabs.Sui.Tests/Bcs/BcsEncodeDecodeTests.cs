namespace MystenLabs.Sui.Tests.Bcs;

using MystenLabs.Sui.Bcs;
using Xunit;

public sealed class BcsEncodeDecodeTests
{
    [Fact]
    public void EncodeStr_Hex_Returns_Lowercase_Hex()
    {
        byte[] data = { 0x0a, 0x2b };
        string encoded = BcsEncodeDecode.EncodeStr(data, BcsEncoding.Hex);
        Assert.Equal("0a2b", encoded);
    }

    [Fact]
    public void EncodeStr_DecodeStr_RoundTrip_Base64()
    {
        byte[] data = { 0x01, 0x02, 0x03 };
        string encoded = BcsEncodeDecode.EncodeStr(data, BcsEncoding.Base64);
        byte[] decoded = BcsEncodeDecode.DecodeStr(encoded.AsSpan(), BcsEncoding.Base64);
        Assert.Equal(data, decoded);
    }

    [Fact]
    public void EncodeStr_DecodeStr_RoundTrip_Base58()
    {
        byte[] data = { 0x01, 0x02, 0x03 };
        string encoded = BcsEncodeDecode.EncodeStr(data, BcsEncoding.Base58);
        byte[] decoded = BcsEncodeDecode.DecodeStr(encoded.AsSpan(), BcsEncoding.Base58);
        Assert.Equal(data, decoded);
    }

    [Fact]
    public void CompareBcsBytes_Equal_Returns_Zero()
    {
        byte[] firstBytes = { 1, 2, 3 };
        byte[] secondBytes = { 1, 2, 3 };
        int result = BcsEncodeDecode.CompareBcsBytes(firstBytes, secondBytes);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CompareBcsBytes_First_Less_Returns_Negative()
    {
        byte[] first = { 1, 2, 2 };
        byte[] second = { 1, 2, 3 };
        int result = BcsEncodeDecode.CompareBcsBytes(first, second);
        Assert.True(result < 0);
    }

    [Fact]
    public void CompareBcsBytes_Second_Shorter_Returns_Positive()
    {
        byte[] first = { 1, 2, 3 };
        byte[] second = { 1, 2 };
        int result = BcsEncodeDecode.CompareBcsBytes(first, second);
        Assert.True(result > 0);
    }
}
