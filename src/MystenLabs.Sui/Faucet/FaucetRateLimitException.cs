namespace MystenLabs.Sui.Faucet;

/// <summary>
/// Thrown when the faucet returns HTTP 429 (too many requests). Callers should retry later.
/// </summary>
public sealed class FaucetRateLimitException : Exception
{
    private const string DefaultMessage =
        "Too many requests from this client have been sent to the faucet. Please retry later.";

    /// <summary>
    /// Creates an instance with the default rate-limit message.
    /// </summary>
    public FaucetRateLimitException()
        : base(DefaultMessage)
    {
    }

    /// <summary>
    /// Creates an instance with the given message.
    /// </summary>
    public FaucetRateLimitException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates an instance with the given message and inner exception.
    /// </summary>
    public FaucetRateLimitException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
