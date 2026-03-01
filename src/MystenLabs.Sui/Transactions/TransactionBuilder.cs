namespace MystenLabs.Sui.Transactions;

using System.Collections.Generic;
using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;

/// <summary>
/// Fluent builder for programmable transactions. Maintains a list of inputs and commands;
/// methods like <see cref="Pure(byte[])"/> and <see cref="Object(CallArg)"/> add inputs and return argument references
/// for use in commands.
/// Implements <see cref="ITransactionDataView"/> for use by <see cref="TransactionPlugin"/>.
/// </summary>
public sealed class TransactionBuilder : ITransactionDataView
{
    private readonly List<CallArg> _inputs = new();
    private readonly List<CommandValue> _commands = new();
    private readonly List<TransactionPlugin> _buildPlugins = new();
    private string? _sender;
    private GasData? _gasData;
    private TransactionExpirationValue? _expiration;

    IList<CallArg> ITransactionDataView.Inputs => _inputs;
    IList<CommandValue> ITransactionDataView.Commands => _commands;
    GasData? ITransactionDataView.GasData { get => _gasData; set => _gasData = value; }
    string? ITransactionDataView.Sender { get => _sender; set => _sender = value; }
    TransactionExpirationValue? ITransactionDataView.Expiration { get => _expiration; set => _expiration = value; }

    /// <summary>
    /// Argument that refers to the gas coin (use as argument in commands).
    /// </summary>
    public static ArgumentValue Gas { get; } = new ArgumentGasCoin();

    /// <summary>
    /// Adds a pure input and returns an argument reference to it.
    /// </summary>
    /// <param name="bytes">BCS-serialized bytes (e.g. from <see cref="Pure.SerializeU64"/> or <see cref="Pure.SerializeAddress"/>).</param>
    /// <returns>Argument that can be used in commands.</returns>
    public ArgumentValue Pure(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        CallArgPure pure = new CallArgPure(bytes);
        _inputs.Add(pure);
        return new ArgumentInput((ushort)(_inputs.Count - 1));
    }

    /// <summary>
    /// Adds a pure input from an existing <see cref="CallArgPure"/> and returns an argument reference.
    /// </summary>
    /// <param name="pure">Pure call argument (e.g. from <see cref="MystenLabs.Sui.Bcs.Pure.U64"/> or <see cref="Inputs.Pure"/>).</param>
    /// <returns>Argument that can be used in commands.</returns>
    public ArgumentValue Pure(CallArgPure pure)
    {
        if (pure == null)
        {
            throw new ArgumentNullException(nameof(pure));
        }

        _inputs.Add(pure);
        return new ArgumentInput((ushort)(_inputs.Count - 1));
    }

    /// <summary>
    /// Adds a pure u64 input (e.g. for amounts).
    /// </summary>
    public ArgumentValue Pure(ulong value)
    {
        return Pure(MystenLabs.Sui.Bcs.Pure.U64(value));
    }

    /// <summary>
    /// Adds a pure address input (normalized). Use for recipient or other address arguments.
    /// </summary>
    public ArgumentValue PureAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentNullException(nameof(address));
        }

        return Pure(MystenLabs.Sui.Bcs.Pure.Address(address));
    }

    /// <summary>
    /// Adds a pure bool input.
    /// </summary>
    public ArgumentValue Pure(bool value)
    {
        return Pure(new CallArgPure(MystenLabs.Sui.Bcs.Pure.SerializeBool(value)));
    }

    /// <summary>
    /// Adds an unresolved pure input; the value will be serialized by the resolution plugin (supports u64, address string, bool, string).
    /// Call <see cref="PrepareForSerializationAsync"/> before <see cref="Build"/>.
    /// </summary>
    public ArgumentValue Pure(object? value)
    {
        _inputs.Add(new CallArgUnresolvedPure(value));
        return new ArgumentInput((ushort)(_inputs.Count - 1));
    }

    /// <summary>
    /// Adds an object input and returns an argument reference to it.
    /// </summary>
    /// <param name="callArg">Object call argument (e.g. from <see cref="Inputs.ObjectRef"/> or <see cref="Inputs.SharedObjectRef"/>).</param>
    /// <returns>Argument that can be used in commands.</returns>
    public ArgumentValue Object(CallArg callArg)
    {
        if (callArg == null)
        {
            throw new ArgumentNullException(nameof(callArg));
        }

        if (callArg is not CallArgObject and not CallArgUnresolvedObject)
        {
            throw new ArgumentException("CallArg must be an object argument (CallArgObject) or unresolved (CallArgUnresolvedObject).", nameof(callArg));
        }

        _inputs.Add(callArg);
        return new ArgumentInput((ushort)(_inputs.Count - 1));
    }

    /// <summary>
    /// Adds an unresolved object input by object ID. Version and digest must be filled by the resolution pipeline
    /// (e.g. <see cref="TransactionResolvingHelpers.ResolveTransactionPlugin"/> or <see cref="IObjectCache"/> in options) before building.
    /// </summary>
    /// <param name="objectId">Object ID (will be normalized).</param>
    /// <returns>Argument that can be used in commands.</returns>
    public ArgumentValue Object(string objectId)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            throw new ArgumentNullException(nameof(objectId));
        }

        string normalized = SuiAddress.Normalize(objectId.AsSpan());
        _inputs.Add(new CallArgUnresolvedObject(normalized));
        return new ArgumentInput((ushort)(_inputs.Count - 1));
    }

    /// <summary>
    /// Adds an owned/immutable object reference as input.
    /// </summary>
    /// <param name="objectId">Object ID.</param>
    /// <param name="version">Object version.</param>
    /// <param name="digest">Object digest (Base58).</param>
    /// <returns>Argument that can be used in commands.</returns>
    public ArgumentValue ObjectRef(string objectId, ulong version, string digest)
    {
        return Object(Inputs.ObjectRef(objectId, version, digest));
    }

    /// <summary>
    /// Adds a shared object reference as input.
    /// </summary>
    /// <param name="objectId">Object ID.</param>
    /// <param name="initialSharedVersion">Initial shared version.</param>
    /// <param name="mutable">Whether the reference is mutable.</param>
    /// <returns>Argument that can be used in commands.</returns>
    public ArgumentValue SharedObjectRef(string objectId, ulong initialSharedVersion, bool mutable)
    {
        return Object(Inputs.SharedObjectRef(objectId, initialSharedVersion, mutable));
    }

    /// <summary>
    /// Adds a receiving object reference as input.
    /// </summary>
    /// <param name="objectId">Object ID.</param>
    /// <param name="version">Object version.</param>
    /// <param name="digest">Object digest (Base58).</param>
    /// <returns>Argument that can be used in commands.</returns>
    public ArgumentValue ReceivingRef(string objectId, ulong version, string digest)
    {
        return Object(Inputs.ReceivingRef(objectId, version, digest));
    }

    /// <summary>
    /// Adds a funds withdrawal input (e.g. for gas from balance).
    /// </summary>
    /// <param name="amount">Amount to withdraw.</param>
    /// <param name="balanceType">Balance type (e.g. "0x2::sui::SUI").</param>
    /// <param name="fromSponsor">If true, withdraw from sponsor.</param>
    /// <returns>Argument that can be used in commands.</returns>
    public ArgumentValue Withdrawal(ulong amount, string? balanceType = null, bool fromSponsor = false)
    {
        CallArg withdrawal = Inputs.FundsWithdrawal(amount, balanceType, fromSponsor);
        _inputs.Add(withdrawal);
        return new ArgumentInput((ushort)(_inputs.Count - 1));
    }

    /// <summary>
    /// Adds an unresolved "coin with balance" input (intent <see cref="SuiIntents.CoinWithBalance"/>).
    /// The resolution plugin (<see cref="TransactionResolvingHelpers.ResolveCoinBalancePlugin"/>) will replace it with a coin object
    /// that has at least the given balance. Requires <see cref="BuildTransactionOptions.Client"/> and sender to be set.
    /// </summary>
    /// <param name="balance">Minimum balance required (e.g. in MIST for SUI).</param>
    /// <param name="coinType">Optional coin type (e.g. "0x2::sui::SUI"). Defaults to SUI when null.</param>
    /// <returns>Argument that can be used in commands.</returns>
    public ArgumentValue CoinWithBalance(ulong balance, string? coinType = null)
    {
        _inputs.Add(new CallArgUnresolvedCoinWithBalance(balance, coinType));
        return new ArgumentInput((ushort)(_inputs.Count - 1));
    }

    /// <summary>
    /// Appends a command and returns an argument reference to its result (for use in subsequent commands).
    /// </summary>
    /// <param name="command">Command (e.g. MoveCall, TransferObjects, SplitCoins).</param>
    /// <returns>Argument referencing this command's result (single result).</returns>
    public ArgumentValue AddCommand(CommandValue command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        _commands.Add(command);
        return new ArgumentResult((ushort)(_commands.Count - 1));
    }

    /// <summary>
    /// Returns a reference to a nested result (result index of a previous command).
    /// </summary>
    /// <param name="commandIndex">Index of the command.</param>
    /// <param name="resultIndex">Index of the result within that command (0 for single-result commands).</param>
    /// <returns>Argument for use in commands.</returns>
    public static ArgumentValue NestedResult(ushort commandIndex, ushort resultIndex)
    {
        return new ArgumentNestedResult(commandIndex, resultIndex);
    }

    /// <summary>
    /// Adds a Move call command.
    /// </summary>
    /// <param name="package">Package address.</param>
    /// <param name="module">Module name.</param>
    /// <param name="function">Function name.</param>
    /// <param name="arguments">Transaction arguments (from <see cref="Pure(byte[])"/>, <see cref="Object(CallArg)"/>, <see cref="Gas"/>, or <see cref="AddCommand"/>).</param>
    /// <param name="typeArguments">Optional type argument strings (e.g. "0x2::sui::SUI"). Parsed and normalized.</param>
    /// <returns>Argument referencing this command's result (for use in subsequent commands).</returns>
    public ArgumentValue MoveCall(
        string package,
        string module,
        string function,
        ArgumentValue[] arguments,
        string[]? typeArguments = null)
    {
        if (string.IsNullOrEmpty(package))
        {
            throw new ArgumentNullException(nameof(package));
        }

        if (string.IsNullOrEmpty(module))
        {
            throw new ArgumentNullException(nameof(module));
        }

        if (string.IsNullOrEmpty(function))
        {
            throw new ArgumentNullException(nameof(function));
        }

        if (arguments == null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        TypeTagValue[] typeTags = typeArguments is { Length: > 0 }
            ? ParseTypeArguments(typeArguments)
            : [];
        string normalizedPackage = SuiAddress.Normalize(package.AsSpan());
        var moveCall = new ProgrammableMoveCall(normalizedPackage, module, function, typeTags, arguments);
        return AddCommand(new CommandMoveCall(moveCall));
    }

    /// <summary>
    /// Adds a Move call command using a single target string (e.g. "0x2::sui::transfer").
    /// </summary>
    /// <param name="target">Full target "package::module::function".</param>
    /// <param name="arguments">Transaction arguments.</param>
    /// <param name="typeArguments">Optional type argument strings.</param>
    /// <returns>Argument referencing this command's result.</returns>
    public ArgumentValue MoveCall(string target, ArgumentValue[] arguments, string[]? typeArguments = null)
    {
        if (string.IsNullOrEmpty(target))
        {
            throw new ArgumentNullException(nameof(target));
        }

        (string package, string module, string function) = ParseMoveCallTarget(target);
        return MoveCall(package, module, function, arguments, typeArguments);
    }

    /// <summary>
    /// Adds a transfer objects command: move objects to an address.
    /// </summary>
    /// <param name="objects">Object arguments to transfer.</param>
    /// <param name="address">Recipient address argument (e.g. from <see cref="Pure(CallArgPure)"/> with <see cref="MystenLabs.Sui.Bcs.Pure.Address"/>).</param>
    /// <returns>Argument referencing this command's result (empty for TransferObjects).</returns>
    public ArgumentValue TransferObjects(ArgumentValue[] objects, ArgumentValue address)
    {
        if (objects == null)
        {
            throw new ArgumentNullException(nameof(objects));
        }

        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        return AddCommand(new CommandTransferObjects(objects, address));
    }

    /// <summary>
    /// Adds a split coins command: splits a coin into multiple amounts. Returns references to the new coins (one per amount).
    /// </summary>
    /// <param name="coin">Coin argument (e.g. gas coin or another coin).</param>
    /// <param name="amounts">Amount arguments (e.g. from <see cref="Pure(CallArgPure)"/> with <see cref="Pure.U64"/>).</param>
    /// <returns>Argument referencing this command's result (use <see cref="NestedResult"/> to reference individual coins).</returns>
    public ArgumentValue SplitCoins(ArgumentValue coin, ArgumentValue[] amounts)
    {
        if (coin == null)
        {
            throw new ArgumentNullException(nameof(coin));
        }

        if (amounts == null)
        {
            throw new ArgumentNullException(nameof(amounts));
        }

        return AddCommand(new CommandSplitCoins(coin, amounts));
    }

    /// <summary>
    /// Adds a merge coins command: merges source coins into the destination coin.
    /// </summary>
    /// <param name="destination">Destination coin argument.</param>
    /// <param name="sources">Source coin arguments.</param>
    /// <returns>Argument referencing this command's result.</returns>
    public ArgumentValue MergeCoins(ArgumentValue destination, ArgumentValue[] sources)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        if (sources == null)
        {
            throw new ArgumentNullException(nameof(sources));
        }

        return AddCommand(new CommandMergeCoins(destination, sources));
    }

    /// <summary>
    /// Adds a MakeMoveVec command: builds a vector of objects (optional type).
    /// </summary>
    /// <param name="elements">Element arguments.</param>
    /// <param name="type">Optional type tag string (e.g. "0x2::coin::Coin&lt;0x2::sui::SUI&gt;"). Null for no type.</param>
    /// <returns>Argument referencing the constructed vector.</returns>
    public ArgumentValue MakeMoveVec(ArgumentValue[] elements, string? type = null)
    {
        if (elements == null)
        {
            throw new ArgumentNullException(nameof(elements));
        }

        TypeTagValue? typeTag = string.IsNullOrWhiteSpace(type)
            ? null
            : TypeTagSerializer.ParseFromStr(type!, normalizeAddress: true);
        return AddCommand(new CommandMakeMoveVec(typeTag, elements));
    }

    /// <summary>
    /// Adds a Publish command: publish Move modules with dependencies.
    /// </summary>
    /// <param name="modules">Compiled module bytes (one array per module).</param>
    /// <param name="dependencies">Package IDs (addresses) of dependencies.</param>
    /// <returns>Argument referencing the publish result (upgrade capability etc.).</returns>
    public ArgumentValue Publish(byte[][] modules, string[] dependencies)
    {
        if (modules == null)
        {
            throw new ArgumentNullException(nameof(modules));
        }

        if (dependencies == null)
        {
            throw new ArgumentNullException(nameof(dependencies));
        }

        return AddCommand(new CommandPublish(modules, dependencies));
    }

    /// <summary>
    /// Adds an Upgrade command: upgrade a package with new modules.
    /// </summary>
    /// <param name="modules">New compiled module bytes.</param>
    /// <param name="dependencies">Package IDs of dependencies.</param>
    /// <param name="package">ID of the package to upgrade.</param>
    /// <param name="ticket">Upgrade ticket argument (from AuthorizeUpgrade or similar).</param>
    /// <returns>Argument referencing the upgrade result.</returns>
    public ArgumentValue Upgrade(byte[][] modules, string[] dependencies, string package, ArgumentValue ticket)
    {
        if (modules == null)
        {
            throw new ArgumentNullException(nameof(modules));
        }

        if (dependencies == null)
        {
            throw new ArgumentNullException(nameof(dependencies));
        }

        if (string.IsNullOrEmpty(package))
        {
            throw new ArgumentNullException(nameof(package));
        }

        if (ticket == null)
        {
            throw new ArgumentNullException(nameof(ticket));
        }

        return AddCommand(new CommandUpgrade(modules, dependencies, package, ticket));
    }

    /// <summary>
    /// Sets the transaction sender.
    /// </summary>
    /// <param name="sender">Sender address (normalized).</param>
    /// <returns>This builder for chaining.</returns>
    public TransactionBuilder SetSender(string sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        return this;
    }

    /// <summary>
    /// Sets gas data (payment, owner, price, budget).
    /// </summary>
    /// <param name="gasData">Gas data.</param>
    /// <returns>This builder for chaining.</returns>
    public TransactionBuilder SetGasData(GasData gasData)
    {
        _gasData = gasData ?? throw new ArgumentNullException(nameof(gasData));
        return this;
    }

    /// <summary>
    /// Sets transaction expiration.
    /// </summary>
    /// <param name="expiration">Expiration (None, Epoch, or ValidDuring).</param>
    /// <returns>This builder for chaining.</returns>
    public TransactionBuilder SetExpiration(TransactionExpirationValue expiration)
    {
        _expiration = expiration ?? throw new ArgumentNullException(nameof(expiration));
        return this;
    }

    /// <summary>
    /// Sets no expiration.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public TransactionBuilder SetExpirationNone()
    {
        _expiration = new TransactionExpirationNone();
        return this;
    }

    /// <summary>
    /// Adds a build plugin that runs during <see cref="PrepareForSerializationAsync"/> (e.g. to resolve unresolved inputs).
    /// </summary>
    /// <param name="plugin">Plugin that receives transaction data, options, and must call next().</param>
    /// <returns>This builder for chaining.</returns>
    public TransactionBuilder AddBuildPlugin(TransactionPlugin plugin)
    {
        if (plugin == null)
        {
            throw new ArgumentNullException(nameof(plugin));
        }

        _buildPlugins.Add(plugin);
        return this;
    }

    /// <summary>
    /// Runs all build plugins (e.g. <see cref="TransactionResolvingHelpers.ResolveTransactionPlugin"/>)
    /// so that unresolved inputs are resolved. Call this before <see cref="Build"/> when using
    /// <see cref="Object(string)"/> or other unresolved inputs.
    /// </summary>
    /// <param name="options">Options (e.g. Client, ObjectCache) passed to each plugin.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    public async Task PrepareForSerializationAsync(
        BuildTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new BuildTransactionOptions();
        cancellationToken.ThrowIfCancellationRequested();

        await RunPluginsAsync(0, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunPluginsAsync(int index, BuildTransactionOptions options, CancellationToken cancellationToken)
    {
        if (index >= _buildPlugins.Count)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        TransactionPlugin plugin = _buildPlugins[index];
        bool nextCalled = false;

        await plugin(this, options, async () =>
        {
            if (nextCalled)
            {
                throw new InvalidOperationException("next() was called more than once in a build plugin.");
            }

            nextCalled = true;
            await RunPluginsAsync(index + 1, options, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        if (!nextCalled)
        {
            throw new InvalidOperationException("next() was not called in a build plugin.");
        }
    }

    /// <summary>
    /// Builds transaction data from the current inputs and commands. Sender, gas data, and at least one command must be set.
    /// All inputs must be resolved (no <see cref="CallArgUnresolvedObject"/> or <see cref="CallArgUnresolvedPure"/>).
    /// Call <see cref="PrepareForSerializationAsync"/> first if you use <see cref="Object(string)"/> or unresolved pure values.
    /// </summary>
    /// <returns>Transaction data ready for serialization and signing.</returns>
    /// <exception cref="InvalidOperationException">Thrown when sender or gas data is not set, when there are no commands, or when any input is unresolved.</exception>
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

        if (_commands.Count == 0)
        {
            throw new InvalidOperationException("At least one command must be added before building.");
        }

        for (int index = 0; index < _inputs.Count; index++)
        {
            if (!CallArgBcs.IsResolved(_inputs[index]))
            {
                throw new InvalidOperationException(
                    $"Input at index {index} is unresolved. Run PrepareForSerializationAsync (with a resolver plugin) before Build, or use resolved inputs only (e.g. ObjectRef, Pure(byte[])).");
            }
        }

        TransactionExpirationValue expiration = _expiration ?? new TransactionExpirationNone();
        return new TransactionDataBuilder()
            .SetSender(_sender)
            .SetGasData(_gasData)
            .SetExpiration(expiration)
            .SetInputs(_inputs.ToArray())
            .SetCommands(_commands.ToArray())
            .Build();
    }

    private static TypeTagValue[] ParseTypeArguments(string[] typeArguments)
    {
        var result = new TypeTagValue[typeArguments.Length];
        for (int index = 0; index < typeArguments.Length; index++)
        {
            result[index] = TypeTagSerializer.ParseFromStr(typeArguments[index], normalizeAddress: true);
        }

        return result;
    }

    private static (string Package, string Module, string Function) ParseMoveCallTarget(string target)
    {
        string[] parts = target.Split("::");
        if (parts.Length != 3)
        {
            throw new ArgumentException(
                "Target must be in the form package::module::function (e.g. 0x2::sui::transfer).",
                nameof(target));
        }

        return (parts[0], parts[1], parts[2]);
    }
}
