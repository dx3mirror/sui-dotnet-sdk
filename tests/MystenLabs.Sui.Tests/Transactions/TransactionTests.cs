namespace MystenLabs.Sui.Tests.Transactions;

using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Keypairs.Ed25519;
using MystenLabs.Sui.Transactions;
using Xunit;

public sealed class TransactionTests
{
    private const string Sender = "0x0000000000000000000000000000000000000001";

    private static TransactionData BuildMinimalTransactionData()
    {
        GasData gasData = new GasData(
            [],
            Sender,
            Price: 1,
            Budget: 1000);
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

        return new TransactionDataBuilder()
            .SetSender(Sender)
            .SetGasData(gasData)
            .SetExpirationNone()
            .SetInputs(inputs)
            .SetCommands(commands)
            .Build();
    }

    [Fact]
    public void Constructor_Null_Data_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Transaction(null!));
    }

    [Fact]
    public void Data_Returns_Same_Instance()
    {
        TransactionData data = BuildMinimalTransactionData();
        var transaction = new Transaction(data);
        Assert.Same(data, transaction.Data);
    }

    [Fact]
    public void GetSerialized_Matches_TransactionDataBuilder_SerializeToBcs()
    {
        TransactionData data = BuildMinimalTransactionData();
        var transaction = new Transaction(data);
        byte[] fromTransaction = transaction.GetSerialized();
        byte[] fromBuilder = TransactionDataBuilder.SerializeToBcs(data);
        Assert.Equal(fromBuilder, fromTransaction);
    }

    [Fact]
    public void GetSerialized_Returns_NonEmpty_Deterministic()
    {
        TransactionData data = BuildMinimalTransactionData();
        var transaction = new Transaction(data);
        byte[] first = transaction.GetSerialized();
        byte[] second = transaction.GetSerialized();
        Assert.NotEmpty(first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Sign_Null_Signer_Throws()
    {
        TransactionData data = BuildMinimalTransactionData();
        var transaction = new Transaction(data);
        Assert.Throws<ArgumentNullException>(() => transaction.Sign(null!));
    }

    [Fact]
    public void Sign_Returns_NonEmpty_Serialized_And_Signature()
    {
        TransactionData data = BuildMinimalTransactionData();
        var transaction = new Transaction(data);
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        (byte[] serialized, string serializedSignature) = transaction.Sign(keypair);
        Assert.NotEmpty(serialized);
        Assert.False(string.IsNullOrEmpty(serializedSignature));
        Assert.Equal(serialized, transaction.GetSerialized());
    }

    [Fact]
    public void Sign_Same_Data_Same_Signer_Produces_Same_Serialized_And_Signature()
    {
        TransactionData data = BuildMinimalTransactionData();
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        var transaction = new Transaction(data);
        (byte[] serialized1, string signature1) = transaction.Sign(keypair);
        (byte[] serialized2, string signature2) = transaction.Sign(keypair);
        Assert.Equal(serialized1, serialized2);
        Assert.Equal(signature1, signature2);
    }

    [Fact]
    public void From_ByteArray_RoundTrips()
    {
        TransactionData data = BuildMinimalTransactionData();
        byte[] bytes = TransactionDataBuilder.SerializeToBcs(data);
        Transaction restored = Transaction.From(bytes);
        Assert.NotNull(restored.Data);
        Assert.Equal(bytes, restored.GetSerialized());
    }

    [Fact]
    public void FromBase64_String_RoundTrips()
    {
        TransactionData data = BuildMinimalTransactionData();
        byte[] bytes = TransactionDataBuilder.SerializeToBcs(data);
        string base64 = MystenLabs.Sui.Utils.Base64.Encode(bytes);
        Transaction restored = Transaction.FromBase64(base64);
        Assert.Equal(bytes, restored.GetSerialized());
    }
}
