namespace MystenLabs.Sui.Tests.Transactions;

using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Transactions;
using Xunit;

public sealed class TransactionDataBuilderTests
{
    private const string Sender = "0x0000000000000000000000000000000000000001";

    private static GasData CreateMinimalGasData()
    {
        return new GasData(
            [],
            Sender,
            Price: 1,
            Budget: 1000);
    }

    [Fact]
    public void Build_Without_Sender_Throws()
    {
        var builder = new TransactionDataBuilder()
            .SetGasData(CreateMinimalGasData())
            .SetInputs([])
            .SetCommands([new CommandMoveCall(new ProgrammableMoveCall("0x2", "sui", "transfer", [], []))]);
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_Without_GasData_Throws()
    {
        var builder = new TransactionDataBuilder()
            .SetSender(Sender)
            .SetInputs([])
            .SetCommands([new CommandMoveCall(new ProgrammableMoveCall("0x2", "sui", "transfer", [], []))]);
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_Without_Commands_Throws()
    {
        var builder = new TransactionDataBuilder()
            .SetSender(Sender)
            .SetGasData(CreateMinimalGasData())
            .SetInputs([]);
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_With_All_Required_Returns_TransactionData()
    {
        CallArg[] inputs = [];
        CommandValue[] commands =
        [
            new CommandMoveCall(new ProgrammableMoveCall(
                "0x0000000000000000000000000000000000000002",
                "sui",
                "transfer",
                [],
                []))
        ];

        TransactionData data = new TransactionDataBuilder()
            .SetSender(Sender)
            .SetGasData(CreateMinimalGasData())
            .SetExpirationNone()
            .SetInputs(inputs)
            .SetCommands(commands)
            .Build();

        Assert.NotNull(data);
        Assert.NotNull(data.V1);
        Assert.Equal(Sender, data.V1.Sender);
        Assert.NotNull(data.V1.Kind);
        Assert.IsType<TransactionKindProgrammable>(data.V1.Kind);
    }

    [Fact]
    public void BuildAndSerialize_Returns_NonEmpty_Bytes()
    {
        CallArg[] inputs = [];
        CommandValue[] commands =
        [
            new CommandMoveCall(new ProgrammableMoveCall(
                "0x0000000000000000000000000000000000000002",
                "sui",
                "transfer",
                [],
                []))
        ];

        byte[] serialized = new TransactionDataBuilder()
            .SetSender(Sender)
            .SetGasData(CreateMinimalGasData())
            .SetExpirationNone()
            .SetInputs(inputs)
            .SetCommands(commands)
            .BuildAndSerialize();

        Assert.NotNull(serialized);
        Assert.NotEmpty(serialized);
    }

    [Fact]
    public void SerializeToBcs_RoundTrip_Parse_Matches()
    {
        CallArg[] inputs = [];
        CommandValue[] commands =
        [
            new CommandMoveCall(new ProgrammableMoveCall(
                "0x0000000000000000000000000000000000000002",
                "sui",
                "transfer",
                [],
                []))
        ];

        TransactionData data = new TransactionDataBuilder()
            .SetSender(Sender)
            .SetGasData(CreateMinimalGasData())
            .SetExpirationNone()
            .SetInputs(inputs)
            .SetCommands(commands)
            .Build();

        byte[] bytes = TransactionDataBuilder.SerializeToBcs(data);
        TransactionData parsed = TransactionDataBcsSerialization.TransactionData.Parse(bytes);
        Assert.NotNull(parsed.V1);
        Assert.Equal(
            SuiAddress.Normalize(data.V1.Sender.AsSpan()),
            SuiAddress.Normalize(parsed.V1.Sender.AsSpan()));
    }
}
