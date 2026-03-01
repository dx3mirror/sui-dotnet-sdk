namespace MystenLabs.Sui.Transactions;

using MystenLabs.Sui.Bcs;

/// <summary>
/// Builds transaction data (V1) from sender, gas data, expiration, inputs, and commands.
/// Serializes to BCS for signing and submission.
/// </summary>
public sealed class TransactionDataBuilder
{
    private string? _sender;
    private GasData? _gasData;
    private TransactionExpirationValue? _expiration;
    private CallArg[]? _inputs;
    private CommandValue[]? _commands;

    /// <summary>
    /// Sets the transaction sender (must be set before building).
    /// </summary>
    /// <param name="sender">Normalized address (0x + 64 hex).</param>
    /// <returns>This builder for chaining.</returns>
    public TransactionDataBuilder SetSender(string sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        return this;
    }

    /// <summary>
    /// Sets gas data (payment objects, owner, price, budget).
    /// </summary>
    public TransactionDataBuilder SetGasData(GasData gasData)
    {
        _gasData = gasData ?? throw new ArgumentNullException(nameof(gasData));
        return this;
    }

    /// <summary>
    /// Sets transaction expiration (None, Epoch, or ValidDuring).
    /// </summary>
    public TransactionDataBuilder SetExpiration(TransactionExpirationValue expiration)
    {
        _expiration = expiration ?? throw new ArgumentNullException(nameof(expiration));
        return this;
    }

    /// <summary>
    /// Sets no expiration (default if not set).
    /// </summary>
    public TransactionDataBuilder SetExpirationNone()
    {
        _expiration = new TransactionExpirationNone();
        return this;
    }

    /// <summary>
    /// Sets the inputs (CallArg array) for the programmable transaction.
    /// </summary>
    public TransactionDataBuilder SetInputs(CallArg[] inputs)
    {
        _inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        return this;
    }

    /// <summary>
    /// Sets the commands for the programmable transaction.
    /// </summary>
    public TransactionDataBuilder SetCommands(CommandValue[] commands)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        return this;
    }

    /// <summary>
    /// Builds the transaction data (V1) with kind = ProgrammableTransaction.
    /// </summary>
    /// <returns>TransactionData with V1 payload.</returns>
    /// <exception cref="InvalidOperationException">Thrown when sender, gas data, inputs, or commands are not set.</exception>
    public TransactionData Build()
    {
        if (string.IsNullOrEmpty(_sender))
        {
            throw new InvalidOperationException("Sender must be set before building.");
        }

        if (_gasData == null)
        {
            throw new InvalidOperationException("Gas data must be set before building.");
        }

        if (_inputs == null)
        {
            throw new InvalidOperationException("Inputs must be set before building.");
        }

        if (_commands == null || _commands.Length == 0)
        {
            throw new InvalidOperationException("At least one command must be set before building.");
        }

        TransactionExpirationValue expiration = _expiration ?? new TransactionExpirationNone();
        var programmable = new ProgrammableTransaction(_inputs, _commands);
        var kind = new TransactionKindProgrammable(programmable);
        var v1 = new TransactionDataV1(kind, _sender, _gasData, expiration);
        return new TransactionData(v1);
    }

    /// <summary>
    /// Builds and serializes transaction data to BCS bytes (for signing and executeTransactionBlock).
    /// </summary>
    /// <returns>BCS-serialized transaction data.</returns>
    public byte[] BuildAndSerialize()
    {
        TransactionData data = Build();
        return SerializeToBcs(data);
    }

    /// <summary>
    /// Serializes transaction data to BCS bytes (for signing and submission).
    /// </summary>
    /// <param name="data">Transaction data (e.g. from <see cref="Build"/>).</param>
    /// <returns>BCS-serialized bytes.</returns>
    public static byte[] SerializeToBcs(TransactionData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return TransactionDataBcsSerialization.TransactionData.Serialize(data, null).ToBytes();
    }
}
