namespace MystenLabs.Sui.Tests.Bcs;

using MystenLabs.Sui.Bcs;
using Xunit;

public sealed class BcsTypesTests
{
    [Fact]
    public void Bool_Serialize_Parse_RoundTrip()
    {
        BcsType<bool> type = Bcs.Bool();
        byte[] bytes = type.Serialize(true).ToBytes();
        bool parsed = type.Parse(bytes);
        Assert.True(parsed);
        bytes = type.Serialize(false).ToBytes();
        parsed = type.Parse(bytes);
        Assert.False(parsed);
    }

    [Fact]
    public void ByteVector_Serialize_Parse_RoundTrip()
    {
        BcsType<byte[]> type = Bcs.ByteVector();
        byte[] value = { 0x01, 0x02, 0x03 };
        byte[] bytes = type.Serialize(value).ToBytes();
        byte[] parsed = type.Parse(bytes);
        Assert.Equal(value, parsed);
    }

    [Fact]
    public void Option_ValueType_None_Serializes_As_Zero()
    {
        BcsType<uint?> type = Bcs.Option(Bcs.U32());
        byte[] bytes = type.Serialize(null).ToBytes();
        Assert.Single(bytes);
        Assert.Equal(0, bytes[0]);
    }

    [Fact]
    public void Option_ValueType_Some_Serialize_Parse_RoundTrip()
    {
        BcsType<uint?> type = Bcs.Option(Bcs.U32());
        byte[] bytes = type.Serialize(42u).ToBytes();
        uint? parsed = type.Parse(bytes);
        Assert.NotNull(parsed);
        Assert.Equal(42u, parsed.Value);
    }

    [Fact]
    public void Vector_U8_Serialize_Parse_RoundTrip()
    {
        BcsType<byte[]> type = Bcs.Vector(Bcs.U8());
        byte[] value = { 1, 2, 3 };
        byte[] bytes = type.Serialize(value).ToBytes();
        byte[] parsed = type.Parse(bytes);
        Assert.Equal(value, parsed);
    }

    [Fact]
    public void Tuple_U8_U32_Serialize_Parse_RoundTrip()
    {
        BcsType<(byte, uint)> type = Bcs.Tuple(Bcs.U8(), Bcs.U32());
        (byte, uint) value = (10, 0x12345678);
        byte[] bytes = type.Serialize(value).ToBytes();
        (byte, uint) parsed = type.Parse(bytes);
        Assert.Equal(value.Item1, parsed.Item1);
        Assert.Equal(value.Item2, parsed.Item2);
    }

    [Fact]
    public void U16_Serialize_Parse_RoundTrip()
    {
        BcsType<ushort> type = Bcs.U16();
        byte[] bytes = type.Serialize(0x1234).ToBytes();
        Assert.Equal(2, bytes.Length);
        ushort parsed = type.Parse(bytes);
        Assert.Equal(0x1234, parsed);
    }

    [Fact]
    public void U64_Serialize_Parse_RoundTrip()
    {
        BcsType<ulong> type = Bcs.U64();
        ulong value = 0x0123_4567_89AB_CDEF;
        byte[] bytes = type.Serialize(value).ToBytes();
        ulong parsed = type.Parse(bytes);
        Assert.Equal(value, parsed);
    }
}
