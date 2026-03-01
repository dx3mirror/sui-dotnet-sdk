namespace MystenLabs.Sui.JsonRpc;

/// <summary>
/// Known Sui network identifiers and default RPC URLs.
/// </summary>
public static class SuiNetwork
{
    /// <summary>
    /// Mainnet RPC URL.
    /// </summary>
    public const string Mainnet = "https://fullnode.mainnet.sui.io:443";

    /// <summary>
    /// Testnet RPC URL.
    /// </summary>
    public const string Testnet = "https://fullnode.testnet.sui.io:443";

    /// <summary>
    /// Devnet RPC URL.
    /// </summary>
    public const string Devnet = "https://fullnode.devnet.sui.io:443";

    /// <summary>
    /// Localnet default RPC URL.
    /// </summary>
    public const string Localnet = "http://127.0.0.1:9000";

    /// <summary>
    /// Returns the default fullnode RPC URL for the given network name.
    /// </summary>
    /// <param name="network">One of: mainnet, testnet, devnet, localnet.</param>
    /// <returns>Base URL for JSON-RPC.</returns>
    public static string GetRpcUrl(string network)
    {
        return network?.ToLowerInvariant() switch
        {
            "mainnet" => Mainnet,
            "testnet" => Testnet,
            "devnet" => Devnet,
            "localnet" => Localnet,
            _ => throw new ArgumentException($"Unknown network: {network}.", nameof(network))
        };
    }
}
