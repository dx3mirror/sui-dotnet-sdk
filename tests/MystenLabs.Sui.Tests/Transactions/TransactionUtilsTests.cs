namespace MystenLabs.Sui.Tests.Transactions;

using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Transactions;
using Xunit;

public sealed class TransactionUtilsTests
{
    [Fact]
    public void GetIdFromCallArg_String_NormalizesAndReturns()
    {
        string? result = TransactionUtils.GetIdFromCallArg("0x1");
        Assert.NotNull(result);
        Assert.StartsWith("0x", result);
    }

    [Fact]
    public void GetIdFromCallArg_String_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(TransactionUtils.GetIdFromCallArg((string?)null));
        Assert.Null(TransactionUtils.GetIdFromCallArg(""));
    }

    [Fact]
    public void GetIdFromCallArg_CallArgObject_ImmOrOwned_ReturnsObjectId()
    {
        CallArg arg = Inputs.ObjectRef("0x2", 1, "E5N2C3xLp4E5N2C3xLp4E5N2C3xLp4E5N2C3xL");
        string? result = TransactionUtils.GetIdFromCallArg(arg);
        Assert.NotNull(result);
        Assert.StartsWith("0x", result);
    }

    [Fact]
    public void GetIdFromCallArg_CallArgObject_Shared_ReturnsObjectId()
    {
        CallArg arg = Inputs.SharedObjectRef("0x3", 1, true);
        string? result = TransactionUtils.GetIdFromCallArg(arg);
        Assert.NotNull(result);
    }

    [Fact]
    public void GetIdFromCallArg_CallArgPure_ReturnsNull()
    {
        CallArg arg = new CallArgPure([1, 2, 3]);
        Assert.Null(TransactionUtils.GetIdFromCallArg(arg));
    }

    [Fact]
    public void GetIdFromCallArg_CallArgNull_ReturnsNull()
    {
        Assert.Null(TransactionUtils.GetIdFromCallArg((CallArg?)null));
    }

    [Fact]
    public void GetIdFromCallArg_CallArgUnresolvedObject_ReturnsObjectId()
    {
        CallArg arg = new CallArgUnresolvedObject("0x0000000000000000000000000000000000000002");
        string? result = TransactionUtils.GetIdFromCallArg(arg);
        Assert.NotNull(result);
        Assert.Contains("2", result);
    }
}
