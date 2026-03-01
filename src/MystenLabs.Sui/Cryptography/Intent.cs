namespace MystenLabs.Sui.Cryptography;

using MystenLabs.Sui.Bcs;

/// <summary>
/// Builds intent-prefixed messages for signing (domain separation).
/// </summary>
public static class Intent
{
    private const byte IntentVersionV0 = 0;
    private const byte AppIdSui = 0;

    /// <summary>
    /// Returns BCS bytes for IntentMessage: intent (scope, version, appId) followed by message.
    /// Layout: scope (ULEB), version (ULEB), appId (ULEB), then raw message bytes.
    /// </summary>
    /// <param name="scope">Intent scope (e.g. TransactionData, PersonalMessage).</param>
    /// <param name="message">Message to wrap.</param>
    /// <returns>Serialized intent message bytes.</returns>
    public static byte[] MessageWithIntent(IntentScope scope, ReadOnlySpan<byte> message)
    {
        var writer = new BcsWriter();
        writer.WriteUleb128((ulong)(int)scope);
        writer.WriteUleb128(IntentVersionV0);
        writer.WriteUleb128(AppIdSui);
        writer.WriteBytes(message);
        return writer.ToBytes();
    }
}
