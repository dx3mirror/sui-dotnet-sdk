namespace MystenLabs.Sui.Tests.Cryptography;

using MystenLabs.Sui.Cryptography;
using Xunit;

public sealed class SuiAddressTests
{
    [Fact]
    public void Normalize_Adds_0x_And_Lowercase()
    {
        string input = "1A2B3C4D5E6F7890ABCDEF1234567890ABCDEF12";
        string normalized = SuiAddress.Normalize(input.AsSpan());
        Assert.StartsWith("0x", normalized);
        Assert.Equal(2 + 64, normalized.Length);
        Assert.Equal(normalized.ToLowerInvariant(), normalized);
    }

    [Fact]
    public void Normalize_With_0x_Prefix_Pads_To_64_Hex_Chars()
    {
        string input = "0x0000000000000000000000000000000000000001";
        string normalized = SuiAddress.Normalize(input.AsSpan());
        Assert.Equal(SuiAddress.NormalizedAddressLength, normalized.Length);
        Assert.StartsWith("0x", normalized);
        Assert.EndsWith("1", normalized);
    }

    [Fact]
    public void Normalize_Short_Address_Pads_With_Zeros()
    {
        string input = "0x1";
        string normalized = SuiAddress.Normalize(input.AsSpan());
        Assert.Equal(2 + 64, normalized.Length);
        Assert.EndsWith("1", normalized);
        Assert.StartsWith("0x", normalized);
    }

    [Fact]
    public void IsValidSuiAddress_ValidAddress_ReturnsTrue()
    {
        Assert.True(SuiAddress.IsValidSuiAddress("0x0000000000000000000000000000000000000000000000000000000000000001"));
        string fullHex = "0x" + new string('a', 64);
        Assert.True(SuiAddress.IsValidSuiAddress(fullHex));
        Assert.True(SuiAddress.IsValidSuiAddress(new string('f', 64))); // 64 hex without 0x
    }

    [Fact]
    public void IsValidSuiAddress_Invalid_ReturnsFalse()
    {
        Assert.False(SuiAddress.IsValidSuiAddress(""));
        Assert.False(SuiAddress.IsValidSuiAddress("0x"));
        Assert.False(SuiAddress.IsValidSuiAddress("0x1")); // too short
        Assert.False(SuiAddress.IsValidSuiAddress("0x" + new string('g', 64))); // non-hex
        Assert.False(SuiAddress.IsValidSuiAddress("0x" + new string('0', 63))); // 63 hex chars
    }

    [Fact]
    public void IsValidSuiObjectId_SameAsAddress_ReturnsTrue_ForValidId()
    {
        string validId = "0x" + new string('0', 62) + "01";
        Assert.True(SuiAddress.IsValidSuiObjectId(validId));
        Assert.False(SuiAddress.IsValidSuiObjectId("not-an-id"));
    }
}
