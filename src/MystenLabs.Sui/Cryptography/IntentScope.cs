namespace MystenLabs.Sui.Cryptography;

/// <summary>
/// Intent scope for signed messages (domain separator).
/// </summary>
public enum IntentScope
{
    /// <summary>
    /// Transaction data to be executed.
    /// </summary>
    TransactionData = 0,

    /// <summary>
    /// Transaction effects.
    /// </summary>
    TransactionEffects = 1,

    /// <summary>
    /// Checkpoint summary.
    /// </summary>
    CheckpointSummary = 2,

    /// <summary>
    /// Personal message (off-chain).
    /// </summary>
    PersonalMessage = 3,
}
