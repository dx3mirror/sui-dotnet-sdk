namespace MystenLabs.Sui.Tests.SuiBcs;

using MystenLabs.Sui.Bcs;
using Xunit;

public sealed class CallArgBcsTests
{
    [Fact]
    public void CallArg_Pure_Serialize_Parse_RoundTrip()
    {
        byte[] payload = [0x01, 0x02, 0x03];
        CallArg value = new CallArgPure(payload);
        byte[] bytes = CallArgBcs.CallArg.Serialize(value).ToBytes();
        CallArg parsed = CallArgBcs.CallArg.Parse(bytes);
        CallArgPure pure = Assert.IsType<CallArgPure>(parsed);
        Assert.Equal(payload, pure.Bytes);
    }

    [Fact]
    public void CallArg_Pure_Empty_ByteArray_RoundTrip()
    {
        CallArg value = new CallArgPure([]);
        byte[] bytes = CallArgBcs.CallArg.Serialize(value).ToBytes();
        CallArg parsed = CallArgBcs.CallArg.Parse(bytes);
        CallArgPure pure = Assert.IsType<CallArgPure>(parsed);
        Assert.Empty(pure.Bytes);
    }

    [Fact]
    public void CallArgBcs_PureFromBase64_PureToBase64_RoundTrip()
    {
        byte[] payload = [0x61, 0x62, 0x63];
        CallArgPure pure = new CallArgPure(payload);
        string base64 = CallArgBcs.PureToBase64(pure);
        CallArgPure decoded = CallArgBcs.PureFromBase64(base64);
        Assert.Equal(pure.Bytes, decoded.Bytes);
    }
}
