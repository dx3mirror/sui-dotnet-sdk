namespace MystenLabs.Sui.Tests.SuiBcs;

using MystenLabs.Sui.Bcs;
using Xunit;

public sealed class TypeTagSerializerTests
{
    [Fact]
    public void ParseFromStr_Bool_Returns_TypeTagBool()
    {
        TypeTagValue tag = TypeTagSerializer.ParseFromStr("bool");
        Assert.IsType<TypeTagBool>(tag);
    }

    [Fact]
    public void ParseFromStr_Address_Returns_TypeTagAddress()
    {
        TypeTagValue tag = TypeTagSerializer.ParseFromStr("address");
        Assert.IsType<TypeTagAddress>(tag);
    }

    [Fact]
    public void ParseFromStr_Vector_U8_Returns_TypeTagVector()
    {
        TypeTagValue tag = TypeTagSerializer.ParseFromStr("vector<u8>");
        Assert.IsType<TypeTagVector>(tag);
        TypeTagVector vector = (TypeTagVector)tag;
        Assert.IsType<TypeTagU8>(vector.Inner);
    }

    [Fact]
    public void TagToString_Bool_Returns_bool()
    {
        TypeTagValue tag = new TypeTagBool();
        string result = TypeTagSerializer.TagToString(tag);
        Assert.Equal("bool", result);
    }

    [Fact]
    public void TagToString_Vector_U8_Returns_vector_u8()
    {
        TypeTagValue tag = new TypeTagVector(new TypeTagU8());
        string result = TypeTagSerializer.TagToString(tag);
        Assert.Equal("vector<u8>", result);
    }

    [Fact]
    public void ParseFromStr_TagToString_RoundTrip_Struct()
    {
        string input = "0x2::coin::Coin<0x2::sui::SUI>";
        TypeTagValue tag = TypeTagSerializer.ParseFromStr(input, normalizeAddress: true);
        string output = TypeTagSerializer.TagToString(tag);
        Assert.StartsWith("0x", output);
        Assert.Contains("::coin::Coin<", output);
        Assert.Contains("::sui::SUI>", output);
        TypeTagValue roundTrip = TypeTagSerializer.ParseFromStr(output, normalizeAddress: true);
        string roundTripStr = TypeTagSerializer.TagToString(roundTrip);
        Assert.Contains("::coin::Coin<", roundTripStr);
        Assert.Contains("::sui::SUI>", roundTripStr);
    }

    [Fact]
    public void NormalizeTypeTag_Returns_Normalized_String()
    {
        string result = TypeTagSerializer.NormalizeTypeTag("0x2::sui::SUI");
        Assert.StartsWith("0x", result);
        Assert.Contains("::sui::SUI", result);
    }

    [Fact]
    public void ParseStructTag_StructType_ReturnsStructTag()
    {
        StructTag tag = TypeTagSerializer.ParseStructTag("0x2::coin::Coin<0x2::sui::SUI>", normalizeAddress: true);
        Assert.NotNull(tag.Address);
        Assert.StartsWith("0x", tag.Address);
        Assert.Equal("coin", tag.Module);
        Assert.Equal("Coin", tag.Name);
        Assert.Single(tag.TypeParams);
    }

    [Fact]
    public void ParseStructTag_Primitive_Throws()
    {
        Assert.Throws<ArgumentException>(() => TypeTagSerializer.ParseStructTag("bool"));
        Assert.Throws<ArgumentException>(() => TypeTagSerializer.ParseStructTag("vector<u8>"));
    }

    [Fact]
    public void NormalizeStructTag_RoundTrip()
    {
        string input = "0x02::sui::SUI";
        string normalized = TypeTagSerializer.NormalizeStructTag(input);
        Assert.StartsWith("0x", normalized);
        Assert.Contains("::sui::SUI", normalized);
        StructTag tag = TypeTagSerializer.ParseStructTag(normalized);
        Assert.Equal("sui", tag.Module);
        Assert.Equal("SUI", tag.Name);
    }
}
