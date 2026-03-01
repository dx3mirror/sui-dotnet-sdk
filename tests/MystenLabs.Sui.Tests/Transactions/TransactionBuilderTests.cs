namespace MystenLabs.Sui.Tests.Transactions;

using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Transactions;
using Xunit;

public sealed class TransactionBuilderTests
{
    private const string Sender = "0x0000000000000000000000000000000000000001";

    private static GasData CreateMinimalGasData()
    {
        return new GasData([], Sender, Price: 1, Budget: 1000);
    }

    [Fact]
    public void Inputs_Pure_ReturnsCallArgPure()
    {
        byte[] bytes = [1, 2, 3];
        CallArgPure result = Inputs.Pure(bytes);
        Assert.NotNull(result);
        Assert.Equal(bytes, result.Bytes);
    }

    [Fact]
    public void Inputs_ObjectRef_NormalizesAddress()
    {
        CallArgObject result = Inputs.ObjectRef("0x1", version: 1, digest: "E5N2C3xLp4E5N2C3xLp4E5N2C3xLp4E5N2C3xL");
        Assert.NotNull(result);
        Assert.IsType<ObjectArgImmOrOwned>(result.Value);
        var reference = ((ObjectArgImmOrOwned)result.Value).Value;
        Assert.StartsWith("0x", reference.ObjectId);
        Assert.Equal(1ul, reference.Version);
    }

    [Fact]
    public void Inputs_FundsWithdrawal_ReturnsCallArgFundsWithdrawal()
    {
        CallArgFundsWithdrawal result = Inputs.FundsWithdrawal(1000);
        Assert.NotNull(result);
        Assert.Equal(1000ul, result.Value.Reservation.MaxAmountU64);
        Assert.IsType<WithdrawFromSender>(result.Value.WithdrawFrom);
    }

    [Fact]
    public void TransactionBuilder_Pure_And_AddCommand_BuildsCorrectData()
    {
        var builder = new TransactionBuilder();
        ArgumentValue amount = builder.Pure(Pure.U64(1));
        ArgumentValue recipient = builder.Pure(Pure.Address(Sender));
        builder.AddCommand(new CommandMoveCall(new ProgrammableMoveCall(
            "0x0000000000000000000000000000000000000002",
            "sui",
            "transfer",
            [],
            [amount, recipient])));
        builder.SetSender(Sender).SetGasData(CreateMinimalGasData());

        TransactionData data = builder.Build();
        Assert.NotNull(data);
        var programmable = (TransactionKindProgrammable)data.V1.Kind;
        Assert.Equal(2, programmable.Value.Inputs.Length);
        Assert.Single(programmable.Value.Commands);
    }

    [Fact]
    public void TransactionBuilder_ObjectRef_AddsInput()
    {
        var builder = new TransactionBuilder();
        ArgumentValue obj = builder.ObjectRef(
            "0x0000000000000000000000000000000000000002",
            version: 42,
            digest: "E5N2C3xLp4E5N2C3xLp4E5N2C3xLp4E5N2C3xL");
        builder.AddCommand(new CommandTransferObjects([obj], builder.Pure(Pure.Address(Sender))));
        builder.SetSender(Sender).SetGasData(CreateMinimalGasData());
        TransactionData data = builder.Build();
        var programmable = (TransactionKindProgrammable)data.V1.Kind;
        Assert.Equal(2, programmable.Value.Inputs.Length);
        Assert.IsType<CallArgObject>(programmable.Value.Inputs[0]);
        Assert.IsType<CallArgPure>(programmable.Value.Inputs[1]);
    }

    [Fact]
    public void TransactionBuilder_Gas_ReturnsArgumentGasCoin()
    {
        Assert.Same(TransactionBuilder.Gas, TransactionBuilder.Gas);
        Assert.IsType<ArgumentGasCoin>(TransactionBuilder.Gas);
    }

    [Fact]
    public void TransactionBuilder_Build_WithoutSender_Throws()
    {
        var builder = new TransactionBuilder();
        builder.Pure(Pure.U64(1));
        builder.AddCommand(new CommandMoveCall(new ProgrammableMoveCall("0x2", "sui", "transfer", [], [])));
        builder.SetGasData(CreateMinimalGasData());
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void TransactionBuilder_Build_WithoutCommands_Throws()
    {
        var builder = new TransactionBuilder();
        builder.SetSender(Sender).SetGasData(CreateMinimalGasData());
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void TransactionBuilder_MoveCall_Target_AddsCommand()
    {
        var builder = new TransactionBuilder();
        ArgumentValue recipient = builder.Pure(Pure.Address(Sender));
        builder.MoveCall("0x2::sui::transfer", [TransactionBuilder.Gas, recipient]);
        builder.SetSender(Sender).SetGasData(CreateMinimalGasData());
        TransactionData data = builder.Build();
        var programmable = (TransactionKindProgrammable)data.V1.Kind;
        Assert.Single(programmable.Value.Commands);
        var moveCall = Assert.IsType<CommandMoveCall>(programmable.Value.Commands[0]);
        Assert.StartsWith("0x", moveCall.MoveCall.Package);
        Assert.Equal(66, moveCall.MoveCall.Package.Length);
        Assert.Equal("sui", moveCall.MoveCall.Module);
        Assert.Equal("transfer", moveCall.MoveCall.Function);
        Assert.Equal(2, moveCall.MoveCall.Arguments.Length);
    }

    [Fact]
    public void TransactionBuilder_MoveCall_InvalidTarget_Throws()
    {
        var builder = new TransactionBuilder();
        Assert.Throws<ArgumentException>(() => builder.MoveCall("invalid", []));
    }

    [Fact]
    public void TransactionBuilder_TransferObjects_AddsCommand()
    {
        var builder = new TransactionBuilder();
        ArgumentValue obj = builder.ObjectRef("0x2", 1, "E5N2C3xLp4E5N2C3xLp4E5N2C3xLp4E5N2C3xL");
        ArgumentValue address = builder.Pure(Pure.Address(Sender));
        builder.TransferObjects([obj], address);
        builder.SetSender(Sender).SetGasData(CreateMinimalGasData());
        TransactionData data = builder.Build();
        var programmable = (TransactionKindProgrammable)data.V1.Kind;
        var cmd = Assert.IsType<CommandTransferObjects>(programmable.Value.Commands[0]);
        Assert.Single(cmd.Objects);
        Assert.Equal(address, cmd.Address);
    }

    [Fact]
    public void TransactionBuilder_Object_String_AddsUnresolvedInput()
    {
        var builder = new TransactionBuilder();
        ArgumentValue obj = builder.Object("0x0000000000000000000000000000000000000002");
        builder.MoveCall("0x2::sui::transfer", [TransactionBuilder.Gas, obj]);
        builder.SetSender(Sender).SetGasData(CreateMinimalGasData());
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public async Task TransactionBuilder_Object_String_ResolvedByCache_BuildsSuccessfully()
    {
        string normalizedId = SuiAddress.Normalize("0x2".AsSpan());
        var cache = new InMemoryObjectCache();
        cache.AddObject(new ObjectCacheEntry(
            normalizedId,
            "1",
            "E5N2C3xLp4E5N2C3xLp4E5N2C3xLp4E5N2C3xL",
            Sender,
            null));
        var builder = new TransactionBuilder();
        builder.AddBuildPlugin(TransactionResolvingHelpers.ResolveTransactionPlugin());
        ArgumentValue obj = builder.Object("0x2");
        builder.MoveCall("0x2::sui::transfer", [TransactionBuilder.Gas, obj]);
        builder.SetSender(Sender).SetGasData(CreateMinimalGasData());
        await builder.PrepareForSerializationAsync(new BuildTransactionOptions { ObjectCache = cache });
        TransactionData data = builder.Build();
        var programmable = (TransactionKindProgrammable)data.V1.Kind;
        Assert.Single(programmable.Value.Inputs);
        Assert.IsType<CallArgObject>(programmable.Value.Inputs[0]);
    }
}
