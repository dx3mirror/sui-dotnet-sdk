namespace MystenLabs.Sui.Tests.SuiBcs;

using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;
using Xunit;

public sealed class SuiBcsTypesTests
{
    private const string KnownAddress = "0x0000000000000000000000000000000000000001";

    [Fact]
    public void Address_Serialize_Parse_RoundTrip()
    {
        byte[] serialized = SuiBcsTypes.Address.Serialize(KnownAddress).ToBytes();
        string parsed = SuiBcsTypes.Address.Parse(serialized);
        Assert.Equal(SuiAddress.Normalize(KnownAddress.AsSpan()), parsed);
    }

    [Fact]
    public void Address_Normalizes_When_Parsing()
    {
        string withExtra = "0x0000000000000000000000000000000000000002";
        byte[] serialized = SuiBcsTypes.Address.Serialize(withExtra).ToBytes();
        string parsed = SuiBcsTypes.Address.Parse(serialized);
        Assert.Equal(2 + 64, parsed.Length);
        Assert.StartsWith("0x", parsed);
    }

    [Fact]
    public void ObjectId_Same_As_Address()
    {
        Assert.Same(SuiBcsTypes.Address, SuiBcsTypes.ObjectId);
    }
}
