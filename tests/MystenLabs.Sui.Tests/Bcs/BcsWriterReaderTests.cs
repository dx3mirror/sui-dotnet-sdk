namespace MystenLabs.Sui.Tests.Bcs;

using MystenLabs.Sui.Bcs;
using Xunit;

public sealed class BcsWriterReaderTests
{
    [Fact]
    public void WriteU8_Read8_RoundTrip()
    {
        var writer = new BcsWriter();
        writer.WriteU8(42);
        byte[] bytes = writer.ToBytes();
        var reader = new BcsReader(bytes);
        Assert.Equal((byte)42, reader.Read8());
    }

    [Fact]
    public void WriteU32_Read32_RoundTrip()
    {
        var writer = new BcsWriter();
        writer.WriteU32(0x1234_5678);
        byte[] bytes = writer.ToBytes();
        var reader = new BcsReader(bytes);
        Assert.Equal(0x1234_5678u, reader.Read32());
    }

    [Fact]
    public void WriteU64_Read64_RoundTrip()
    {
        var writer = new BcsWriter();
        writer.WriteU64(0x0123_4567_89AB_CDEF);
        byte[] bytes = writer.ToBytes();
        var reader = new BcsReader(bytes);
        Assert.Equal(0x0123_4567_89AB_CDEFul, reader.Read64());
    }

    [Fact]
    public void WriteUleb128_ReadUleb128_RoundTrip()
    {
        var writer = new BcsWriter();
        writer.WriteUleb128(300);
        byte[] bytes = writer.ToBytes();
        var reader = new BcsReader(bytes);
        Assert.Equal(300ul, reader.ReadUleb128());
    }

    [Fact]
    public void Bcs_U8_Serialize_Parse()
    {
        BcsType<byte> u8 = Bcs.U8();
        SerializedBcs<byte> serialized = u8.Serialize(200);
        byte[] raw = serialized.ToBytes();
        Assert.Single(raw);
        Assert.Equal(200, raw[0]);
        byte parsed = u8.Parse(raw);
        Assert.Equal(200, parsed);
    }

    [Fact]
    public void Bcs_String_Serialize_Parse()
    {
        BcsType<string> str = Bcs.String();
        SerializedBcs<string> serialized = str.Serialize("hello");
        string parsed = str.Parse(serialized.ToBytes());
        Assert.Equal("hello", parsed);
    }
}
