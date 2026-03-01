namespace MystenLabs.Sui.Tests.Transactions;

using MystenLabs.Sui.Transactions;
using Xunit;

public sealed class TransactionHasherTests
{
    [Fact]
    public void GetDigestToSign_Returns_32_Bytes()
    {
        byte[] transaction = [0x01, 0x02, 0x03];
        byte[] digest = TransactionHasher.GetDigestToSign(transaction);
        Assert.Equal(32, digest.Length);
    }

    [Fact]
    public void GetDigestToSign_Same_Input_Same_Output()
    {
        byte[] transaction = [0x61, 0x62, 0x63];
        byte[] digest1 = TransactionHasher.GetDigestToSign(transaction);
        byte[] digest2 = TransactionHasher.GetDigestToSign(transaction);
        Assert.Equal(digest1, digest2);
    }

    [Fact]
    public void GetDigestToSign_Different_Input_Different_Output()
    {
        byte[] transaction1 = [0x01];
        byte[] transaction2 = [0x02];
        byte[] digest1 = TransactionHasher.GetDigestToSign(transaction1);
        byte[] digest2 = TransactionHasher.GetDigestToSign(transaction2);
        Assert.NotEqual(digest1, digest2);
    }

    [Fact]
    public void HashTypedData_Includes_TypeTag_Prefix()
    {
        byte[] data = [0x01];
        byte[] hash = TransactionHasher.HashTypedData("CustomTag", data);
        Assert.Equal(32, hash.Length);
        byte[] hash2 = TransactionHasher.HashTypedData("OtherTag", data);
        Assert.NotEqual(hash, hash2);
    }
}
