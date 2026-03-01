namespace MystenLabs.Sui.Transactions;

using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Utils;

/// <summary>
/// Wraps built transaction data for signing and submission.
/// Use <see cref="TransactionDataBuilder"/> or <see cref="TransactionBuilder"/> to build, or <see cref="From"/>, <see cref="FromBase64"/>, <see cref="FromKind"/>, <see cref="FromJson"/> to deserialize.
/// </summary>
public sealed class Transaction
{
    private readonly TransactionData _data;

    /// <summary>
    /// Creates a transaction wrapper from built transaction data.
    /// </summary>
    /// <param name="data">Transaction data (e.g. from <see cref="TransactionDataBuilder.Build"/>).</param>
    public Transaction(TransactionData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// Deserializes transaction data from BCS bytes and returns a <see cref="Transaction"/> (e.g. for signing or inspection).
    /// </summary>
    /// <param name="bytes">BCS-serialized transaction data (full payload: kind, sender, gas, expiration).</param>
    /// <returns>Transaction wrapping the deserialized data.</returns>
    public static Transaction From(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        TransactionData data = TransactionDataBcsSerialization.TransactionData.Parse(bytes);
        return new Transaction(data);
    }

    /// <summary>
    /// Deserializes transaction data from Base64-encoded BCS bytes (e.g. from <see cref="TransactionDataBuilder.SerializeToBcs"/> then Base64.Encode).
    /// </summary>
    /// <param name="base64">Base64 string of BCS transaction data.</param>
    /// <returns>Transaction wrapping the deserialized data.</returns>
    public static Transaction FromBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new ArgumentNullException(nameof(base64));
        }

        byte[] bytes = Base64.Decode(base64.Trim().AsSpan());
        return From(bytes);
    }

    /// <summary>
    /// Placeholder address used when deserializing kind-only or when sender/gas are not set (e.g. <see cref="FromKind"/>).
    /// </summary>
    public static readonly string PlaceholderAddress = "0x0000000000000000000000000000000000000000000000000000000000000000";

    /// <summary>
    /// Deserializes transaction-kind-only BCS bytes (programmable transaction: inputs + commands, no sender/gas/expiration).
    /// The returned transaction has placeholder sender and gas; set them or use for signing only the kind.
    /// </summary>
    /// <param name="kindBytes">BCS bytes of the transaction kind (variant 0 = ProgrammableTransaction, then inputs and commands).</param>
    /// <returns>Transaction with the deserialized kind and placeholder sender/gas/expiration.</returns>
    public static Transaction FromKind(byte[] kindBytes)
    {
        if (kindBytes == null || kindBytes.Length == 0)
        {
            throw new ArgumentNullException(nameof(kindBytes));
        }

        var reader = new BcsReader(kindBytes);
        TransactionKindValue kind = TransactionDataBcsSerialization.TransactionKind.Read(reader);
        GasData placeholderGas = new GasData([], PlaceholderAddress, 0, 0);
        var v1 = new TransactionDataV1(kind, PlaceholderAddress, placeholderGas, new TransactionExpirationNone());
        return new Transaction(new TransactionData(v1));
    }

    /// <summary>
    /// Deserializes a transaction from JSON (same schema as TypeScript SDK: optional "data" wrapper, then kind, sender, gasData, expiration).
    /// </summary>
    /// <param name="json">JSON string (e.g. from TS SDK transaction.toJSON()).</param>
    /// <returns>Transaction with parsed data.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="System.Text.Json.JsonException">If JSON structure is invalid or unsupported.</exception>
    public static Transaction FromJson(string json)
    {
        return TransactionJson.FromJson(json);
    }

    /// <summary>
    /// Gets the underlying transaction data.
    /// </summary>
    public TransactionData Data => _data;

    /// <summary>
    /// Serializes the transaction to BCS bytes (for signing and executeTransactionBlock).
    /// </summary>
    /// <returns>BCS-serialized transaction data.</returns>
    public byte[] GetSerialized()
    {
        return TransactionDataBuilder.SerializeToBcs(_data);
    }

    /// <summary>
    /// Signs the transaction with the given signer (digest = hash of serialized data with intent).
    /// </summary>
    /// <param name="signer">Signer (e.g. Ed25519Keypair).</param>
    /// <returns>Serialized transaction bytes and the serialized signature (base64) for executeTransactionBlock.</returns>
    public (byte[] SerializedTransaction, string SerializedSignature) Sign(Signer signer)
    {
        if (signer == null)
        {
            throw new ArgumentNullException(nameof(signer));
        }

        byte[] serialized = GetSerialized();
        byte[] digest = TransactionHasher.GetDigestToSign(serialized);
        byte[] signature = signer.Sign(digest);
        string serializedSignature = Signature.ToSerializedSignature(
            signer.GetKeyScheme(),
            signature,
            signer.GetPublicKey());
        return (serialized, serializedSignature);
    }
}
