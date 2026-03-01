namespace MystenLabs.Sui.Tests.Utils;

using MystenLabs.Sui.Utils;
using Xunit;

public sealed class TaskWithResolversTests
{
    [Fact]
    public async Task Create_Resolve_CompletesWithValue()
    {
        TaskWithResolvers<int> resolvers = TaskWithResolvers<int>.Create();
        resolvers.Resolve(42);
        int result = await resolvers.Task;
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Create_Reject_CompletesWithException()
    {
        TaskWithResolvers<int> resolvers = TaskWithResolvers<int>.Create();
        var expected = new InvalidOperationException("test");
        resolvers.Reject(expected);
        InvalidOperationException? caught = await Assert.ThrowsAsync<InvalidOperationException>(async () => await resolvers.Task);
        Assert.Same(expected, caught);
    }
}
