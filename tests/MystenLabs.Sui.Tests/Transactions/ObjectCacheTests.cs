namespace MystenLabs.Sui.Tests.Transactions;

using MystenLabs.Sui.Transactions;
using Xunit;

public sealed class ObjectCacheTests
{
    private const string ObjectId1 = "0x0000000000000000000000000000000000000001";
    private const string ObjectId2 = "0x0000000000000000000000000000000000000002";

    [Fact]
    public void GetObject_Empty_ReturnsNull()
    {
        var cache = new InMemoryObjectCache();
        Assert.Null(cache.GetObject(ObjectId1));
    }

    [Fact]
    public void AddObject_ThenGetObject_ReturnsEntry()
    {
        var cache = new InMemoryObjectCache();
        var entry = new ObjectCacheEntry(ObjectId1, "1", "Digest1", "0xowner", null);
        cache.AddObject(entry);
        ObjectCacheEntry? result = cache.GetObject(ObjectId1);
        Assert.NotNull(result);
        Assert.Equal(ObjectId1, result.ObjectId);
        Assert.Equal("1", result.Version);
        Assert.Equal("0xowner", result.Owner);
    }

    [Fact]
    public void AddObject_Shared_StoredInSharedBucket()
    {
        var cache = new InMemoryObjectCache();
        var entry = new ObjectCacheEntry(ObjectId1, "1", "D", null, "1");
        cache.AddObject(entry);
        Assert.NotNull(cache.GetObject(ObjectId1));
        Assert.Null(cache.GetObject(ObjectId1)!.Owner);
        Assert.Equal("1", cache.GetObject(ObjectId1)!.InitialSharedVersion);
    }

    [Fact]
    public void DeleteObject_RemovesEntry()
    {
        var cache = new InMemoryObjectCache();
        cache.AddObject(new ObjectCacheEntry(ObjectId1, "1", "D", null, null));
        cache.DeleteObject(ObjectId1);
        Assert.Null(cache.GetObject(ObjectId1));
    }

    [Fact]
    public void GetObjects_ReturnsInOrder()
    {
        var cache = new InMemoryObjectCache();
        cache.AddObject(new ObjectCacheEntry(ObjectId1, "1", "D1", null, null));
        cache.AddObject(new ObjectCacheEntry(ObjectId2, "2", "D2", null, null));
        ObjectCacheEntry?[] result = cache.GetObjects([ObjectId1, ObjectId2, "0xnone"]);
        Assert.Equal(3, result.Length);
        Assert.NotNull(result[0]);
        Assert.NotNull(result[1]);
        Assert.Null(result[2]);
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var cache = new InMemoryObjectCache();
        cache.AddObject(new ObjectCacheEntry(ObjectId1, "1", "D", null, null));
        cache.Clear();
        Assert.Null(cache.GetObject(ObjectId1));
    }
}
