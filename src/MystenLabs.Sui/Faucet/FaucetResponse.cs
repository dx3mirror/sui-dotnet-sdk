namespace MystenLabs.Sui.Faucet;

using System.Text.Json.Serialization;

/// <summary>
/// Information about one coin sent by the faucet (amount, object id, transfer transaction digest).
/// </summary>
public sealed class FaucetCoinInfo
{
    /// <summary>
    /// Amount of SUI (or gas) sent.
    /// </summary>
    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    /// <summary>
    /// Object ID of the coin created.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Digest of the transfer transaction.
    /// </summary>
    [JsonPropertyName("transferTxDigest")]
    public string TransferTxDigest { get; set; } = string.Empty;
}

/// <summary>
/// Successful result of a v2 faucet request (gas/SUI sent to the recipient).
/// </summary>
public sealed class FaucetRequestSuiResult
{
    /// <summary>
    /// Coins sent to the recipient (at least one on success).
    /// </summary>
    public IReadOnlyList<FaucetCoinInfo> CoinsSent { get; }

    /// <summary>
    /// Creates a result from the list of coins sent.
    /// </summary>
    internal FaucetRequestSuiResult(IReadOnlyList<FaucetCoinInfo> coinsSent)
    {
        CoinsSent = coinsSent ?? throw new ArgumentNullException(nameof(coinsSent));
    }
}
