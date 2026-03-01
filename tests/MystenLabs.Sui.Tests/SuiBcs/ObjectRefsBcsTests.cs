namespace MystenLabs.Sui.Tests.SuiBcs;

using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Utils;
using Xunit;

public sealed class ObjectRefsBcsTests
{
    private static readonly string KnownObjectId =
        SuiAddress.Normalize("0x0000000000000000000000000000000000000001".AsSpan());

    private static string CreateValidDigestBase58()
    {
        byte[] digestBytes = new byte[32];
        digestBytes[0] = 1;
        return Base58.Encode(digestBytes);
    }

    [Fact]
    public void SuiObjectRef_Serialize_Parse_RoundTrip()
    {
        string digest = CreateValidDigestBase58();
        var reference = new SuiObjectRef(KnownObjectId, 1, digest);
        byte[] bytes = SuiObjectRefBcs.SuiObjectRef.Serialize(reference).ToBytes();
        SuiObjectRef parsed = SuiObjectRefBcs.SuiObjectRef.Parse(bytes);
        Assert.Equal(
            SuiAddress.Normalize(reference.ObjectId.AsSpan()),
            SuiAddress.Normalize(parsed.ObjectId.AsSpan()));
        Assert.Equal(reference.Version, parsed.Version);
        Assert.Equal(reference.Digest, parsed.Digest);
    }

    [Fact]
    public void SharedObjectRef_Serialize_Parse_RoundTrip()
    {
        var reference = new SharedObjectRef(KnownObjectId, 100, Mutable: true);
        byte[] bytes = SuiObjectRefBcs.SharedObjectRef.Serialize(reference).ToBytes();
        SharedObjectRef parsed = SuiObjectRefBcs.SharedObjectRef.Parse(bytes);
        Assert.Equal(
            SuiAddress.Normalize(reference.ObjectId.AsSpan()),
            SuiAddress.Normalize(parsed.ObjectId.AsSpan()));
        Assert.Equal(reference.InitialSharedVersion, parsed.InitialSharedVersion);
        Assert.Equal(reference.Mutable, parsed.Mutable);
    }

    [Fact]
    public void SharedObjectRef_Mutable_False_RoundTrip()
    {
        var reference = new SharedObjectRef(KnownObjectId, 0, Mutable: false);
        byte[] bytes = SuiObjectRefBcs.SharedObjectRef.Serialize(reference).ToBytes();
        SharedObjectRef parsed = SuiObjectRefBcs.SharedObjectRef.Parse(bytes);
        Assert.Equal(
            SuiAddress.Normalize(reference.ObjectId.AsSpan()),
            SuiAddress.Normalize(parsed.ObjectId.AsSpan()));
        Assert.False(parsed.Mutable);
    }

    [Fact]
    public void ObjectDigest_Serialize_Parse_RoundTrip()
    {
        string digest = CreateValidDigestBase58();
        byte[] bytes = ObjectDigestBcs.ObjectDigest.Serialize(digest).ToBytes();
        string parsed = ObjectDigestBcs.ObjectDigest.Parse(bytes);
        Assert.Equal(digest, parsed);
    }
}
