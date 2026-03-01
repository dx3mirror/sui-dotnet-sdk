namespace MystenLabs.Sui.Tests.Cryptography;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Utils;
using Xunit;

public sealed class TransactionDigestTests
{
    [Fact]
    public void IsValid_Valid32ByteBase58_ReturnsTrue()
    {
        byte[] thirtyTwoBytes = new byte[TransactionDigest.DigestLengthBytes];
        thirtyTwoBytes[0] = 1;
        string base58 = Base58.Encode(thirtyTwoBytes);
        Assert.True(TransactionDigest.IsValid(base58));
    }

    [Fact]
    public void IsValid_InvalidLength_ReturnsFalse()
    {
        byte[] shortBytes = { 1, 2, 3 };
        string base58 = Base58.Encode(shortBytes);
        Assert.False(TransactionDigest.IsValid(base58));
    }

    [Fact]
    public void IsValid_Empty_ReturnsFalse()
    {
        Assert.False(TransactionDigest.IsValid(""));
        Assert.False(TransactionDigest.IsValid("   "));
    }

    [Fact]
    public void IsValid_InvalidBase58_ReturnsFalse()
    {
        Assert.False(TransactionDigest.IsValid("0OIl")); // invalid Base58 chars
    }
}
