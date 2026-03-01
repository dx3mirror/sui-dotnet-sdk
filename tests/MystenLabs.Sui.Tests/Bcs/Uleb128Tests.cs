namespace MystenLabs.Sui.Tests.Bcs;

using MystenLabs.Sui.Bcs;
using Xunit;

public sealed class Uleb128Tests
{
    [Fact]
    public void Encode_Zero_Returns_One_Byte()
    {
        byte[] encoded = Uleb128.Encode(0);
        Assert.Single(encoded);
        Assert.Equal(0, encoded[0]);
    }

    [Fact]
    public void Encode_Decode_RoundTrip()
    {
        ulong[] values = { 0, 1, 127, 128, 255, 256, 16384, 0x3FFF_FFFF_FFFF_FFFF };
        foreach (ulong value in values)
        {
            byte[] encoded = Uleb128.Encode(value);
            (ulong decoded, int length) = Uleb128.Decode(encoded);
            Assert.Equal(value, decoded);
            Assert.Equal(encoded.Length, length);
        }
    }

    [Fact]
    public void Decode_Throws_On_Empty()
    {
        Assert.Throws<InvalidOperationException>(() => Uleb128.Decode(ReadOnlySpan<byte>.Empty));
    }
}
