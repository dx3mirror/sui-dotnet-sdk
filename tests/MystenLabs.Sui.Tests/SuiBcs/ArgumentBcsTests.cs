namespace MystenLabs.Sui.Tests.SuiBcs;

using MystenLabs.Sui.Bcs;
using Xunit;

public sealed class ArgumentBcsTests
{
    [Fact]
    public void Argument_GasCoin_Serialize_Parse_RoundTrip()
    {
        ArgumentValue value = new ArgumentGasCoin();
        byte[] bytes = ArgumentBcs.Argument.Serialize(value).ToBytes();
        ArgumentValue parsed = ArgumentBcs.Argument.Parse(bytes);
        Assert.IsType<ArgumentGasCoin>(parsed);
    }

    [Fact]
    public void Argument_Input_Serialize_Parse_RoundTrip()
    {
        ArgumentValue value = new ArgumentInput(5);
        byte[] bytes = ArgumentBcs.Argument.Serialize(value).ToBytes();
        ArgumentValue parsed = ArgumentBcs.Argument.Parse(bytes);
        ArgumentInput input = Assert.IsType<ArgumentInput>(parsed);
        Assert.Equal((ushort)5, input.Index);
    }

    [Fact]
    public void Argument_Result_Serialize_Parse_RoundTrip()
    {
        ArgumentValue value = new ArgumentResult(3);
        byte[] bytes = ArgumentBcs.Argument.Serialize(value).ToBytes();
        ArgumentValue parsed = ArgumentBcs.Argument.Parse(bytes);
        ArgumentResult result = Assert.IsType<ArgumentResult>(parsed);
        Assert.Equal((ushort)3, result.Index);
    }

    [Fact]
    public void Argument_NestedResult_Serialize_Parse_RoundTrip()
    {
        ArgumentValue value = new ArgumentNestedResult(1, 0);
        byte[] bytes = ArgumentBcs.Argument.Serialize(value).ToBytes();
        ArgumentValue parsed = ArgumentBcs.Argument.Parse(bytes);
        ArgumentNestedResult nested = Assert.IsType<ArgumentNestedResult>(parsed);
        Assert.Equal((ushort)1, nested.CommandIndex);
        Assert.Equal((ushort)0, nested.ResultIndex);
    }
}
