namespace MystenLabs.Sui.Tests.Utils;

using MystenLabs.Sui.Utils;
using Xunit;

public sealed class ChunkTests
{
    [Fact]
    public void ToChunks_Splits_Evenly()
    {
        int[] array = { 1, 2, 3, 4, 5, 6 };
        int[][] chunks = Chunk.ToChunks(array, 2);
        Assert.Equal(3, chunks.Length);
        Assert.Equal(new[] { 1, 2 }, chunks[0]);
        Assert.Equal(new[] { 3, 4 }, chunks[1]);
        Assert.Equal(new[] { 5, 6 }, chunks[2]);
    }

    [Fact]
    public void ToChunks_LastChunk_Can_Be_Smaller()
    {
        int[] array = { 1, 2, 3, 4, 5 };
        int[][] chunks = Chunk.ToChunks(array, 2);
        Assert.Equal(3, chunks.Length);
        Assert.Equal(new[] { 1, 2 }, chunks[0]);
        Assert.Equal(new[] { 3, 4 }, chunks[1]);
        Assert.Equal(new[] { 5 }, chunks[2]);
    }

    [Fact]
    public void ToChunks_Empty_Returns_Empty()
    {
        int[][] chunks = Chunk.ToChunks<int>([], 2);
        Assert.Empty(chunks);
    }

    [Fact]
    public void ToChunks_SizeOne_Returns_Each_Element()
    {
        int[] array = { 1, 2, 3 };
        int[][] chunks = Chunk.ToChunks(array, 1);
        Assert.Equal(3, chunks.Length);
        Assert.Equal(new[] { 1 }, chunks[0]);
        Assert.Equal(new[] { 2 }, chunks[1]);
        Assert.Equal(new[] { 3 }, chunks[2]);
    }
}
