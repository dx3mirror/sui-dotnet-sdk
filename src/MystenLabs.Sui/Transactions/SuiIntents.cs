namespace MystenLabs.Sui.Transactions;

using MystenLabs.Sui.Bcs;

/// <summary>
/// Intent identifiers for transaction inputs (e.g. resolved by plugins before build/serialization).
/// </summary>
public static class SuiIntents
{
    /// <summary>
    /// Intent: input should be resolved to a coin object with at least the specified balance.
    /// Use with <see cref="CallArgUnresolvedCoinWithBalance"/>; resolved by <see cref="TransactionResolvingHelpers.ResolveCoinBalancePlugin"/>.
    /// </summary>
    public const string CoinWithBalance = "CoinWithBalance";
}
