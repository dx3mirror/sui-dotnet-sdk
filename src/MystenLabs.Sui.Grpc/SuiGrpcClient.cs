namespace MystenLabs.Sui.Grpc;

using global::Grpc.Net.Client;

/// <summary>
/// Sui gRPC client. Connects to a Sui full node gRPC endpoint (recommended over JSON-RPC for new integrations).
/// Default endpoints: mainnet fullnode.mainnet.sui.io:443, testnet fullnode.testnet.sui.io:443, devnet fullnode.devnet.sui.io:443.
/// </summary>
public sealed class SuiGrpcClient
{
    /// <summary>
    /// Default mainnet gRPC address.
    /// </summary>
    public const string DefaultMainnetAddress = "https://fullnode.mainnet.sui.io:443";

    /// <summary>
    /// Default testnet gRPC address.
    /// </summary>
    public const string DefaultTestnetAddress = "https://fullnode.testnet.sui.io:443";

    /// <summary>
    /// Default devnet gRPC address.
    /// </summary>
    public const string DefaultDevnetAddress = "https://fullnode.devnet.sui.io:443";

    /// <summary>
    /// Default local full node gRPC address.
    /// </summary>
    public const string DefaultLocalAddress = "http://127.0.0.1:9000";

    private readonly GrpcChannel _channel;

    /// <summary>
    /// State service (list objects, get balance, list balances, list dynamic fields, get coin info).
    /// </summary>
    public global::Sui.Rpc.V2.StateService.StateServiceClient State => new(_channel);

    /// <summary>
    /// Ledger service (checkpoints, transaction blocks, service info).
    /// </summary>
    public global::Sui.Rpc.V2.LedgerService.LedgerServiceClient Ledger => new(_channel);

    /// <summary>
    /// Transaction execution service (execute transaction block, dry run, etc.).
    /// </summary>
    public global::Sui.Rpc.V2.TransactionExecutionService.TransactionExecutionServiceClient TransactionExecution => new(_channel);

    /// <summary>
    /// Creates a gRPC client for the given address.
    /// </summary>
    /// <param name="address">Base address (e.g. https://fullnode.mainnet.sui.io:443).</param>
    /// <param name="httpHandler">Optional HTTP handler (e.g. for auth or custom options).</param>
    public SuiGrpcClient(string address, HttpMessageHandler? httpHandler = null)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentNullException(nameof(address));
        }

        _channel = httpHandler != null
            ? global::Grpc.Net.Client.GrpcChannel.ForAddress(address.TrimEnd('/'), new global::Grpc.Net.Client.GrpcChannelOptions { HttpHandler = httpHandler })
            : global::Grpc.Net.Client.GrpcChannel.ForAddress(address.TrimEnd('/'));
    }

    /// <summary>
    /// Creates a gRPC client using an existing channel (e.g. for sharing or custom configuration).
    /// </summary>
    /// <param name="channel">Shared or pre-configured channel.</param>
    public SuiGrpcClient(global::Grpc.Net.Client.GrpcChannel channel)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }
}
