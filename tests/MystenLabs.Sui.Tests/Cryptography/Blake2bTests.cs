namespace MystenLabs.Sui.Tests.Cryptography;

using MystenLabs.Sui.Cryptography;
using Xunit;

public sealed class Blake2bTests
{
    [Fact]
    public void Hash256_Returns_32_Bytes()
    {
        byte[] data = [0x01, 0x02, 0x03];
        byte[] hash = Blake2b.Hash256(data);
        Assert.Equal(32, hash.Length);
    }

    [Fact]
    public void Hash256_Same_Input_Same_Output()
    {
        byte[] data = [0x61, 0x62, 0x63];
        byte[] hash1 = Blake2b.Hash256(data);
        byte[] hash2 = Blake2b.Hash256(data);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Hash256_Different_Input_Different_Output()
    {
        byte[] data1 = [0x61];
        byte[] data2 = [0x62];
        byte[] hash1 = Blake2b.Hash256(data1);
        byte[] hash2 = Blake2b.Hash256(data2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash256_Empty_Input_Produces_Deterministic_Hash()
    {
        byte[] hash = Blake2b.Hash256([]);
        Assert.Equal(32, hash.Length);
        byte[] hash2 = Blake2b.Hash256([]);
        Assert.Equal(hash, hash2);
    }
}
