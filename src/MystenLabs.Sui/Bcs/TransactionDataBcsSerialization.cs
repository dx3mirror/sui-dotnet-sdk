namespace MystenLabs.Sui.Bcs;

/// <summary>
/// BCS serialization for GasData, ValidDuring, TransactionExpiration, TransactionKind, TransactionDataV1, and TransactionData.
/// </summary>
public static class TransactionDataBcsSerialization
{
    private const int TransactionKindVariantProgrammableTransaction = 0;
    private const int TransactionExpirationVariantNone = 0;
    private const int TransactionExpirationVariantEpoch = 1;
    private const int TransactionExpirationVariantValidDuring = 2;
    private const int TransactionDataVariantV1 = 0;

    private static BcsType<ulong?> OptionU64 { get; } = Bcs.Option(Bcs.U64());
    private static BcsType<SuiObjectRef[]> SuiObjectRefVector { get; } = Bcs.Vector(SuiObjectRefBcs.SuiObjectRef);

    /// <summary>
    /// BCS type for GasData (struct: payment, owner, price, budget).
    /// </summary>
    public static BcsType<GasData> GasData { get; } = new BcsType<GasData>(
        "GasData",
        reader =>
        {
            SuiObjectRef[] payment = SuiObjectRefVector.Read(reader);
            string owner = SuiBcsTypes.Address.Read(reader);
            ulong price = reader.Read64();
            ulong budget = reader.Read64();
            return new GasData(payment, owner, price, budget);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            SuiObjectRefVector.Write(value.Payment, writer);
            SuiBcsTypes.Address.Write(value.Owner, writer);
            writer.WriteU64(value.Price);
            writer.WriteU64(value.Budget);
        },
        _ => null,
        value =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        });

    /// <summary>
    /// BCS type for ValidDuring (struct: minEpoch?, maxEpoch?, minTimestamp?, maxTimestamp?, chain, nonce).
    /// </summary>
    public static BcsType<ValidDuring> ValidDuring { get; } = new BcsType<ValidDuring>(
        "ValidDuring",
        reader =>
        {
            ulong? minEpoch = OptionU64.Read(reader);
            ulong? maxEpoch = OptionU64.Read(reader);
            ulong? minTimestamp = OptionU64.Read(reader);
            ulong? maxTimestamp = OptionU64.Read(reader);
            string chain = ObjectDigestBcs.ObjectDigest.Read(reader);
            uint nonce = reader.Read32();
            return new ValidDuring(minEpoch, maxEpoch, minTimestamp, maxTimestamp, chain, nonce);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            OptionU64.Write(value.MinEpoch, writer);
            OptionU64.Write(value.MaxEpoch, writer);
            OptionU64.Write(value.MinTimestamp, writer);
            OptionU64.Write(value.MaxTimestamp, writer);
            ObjectDigestBcs.ObjectDigest.Write(value.Chain, writer);
            writer.WriteU32(value.Nonce);
        },
        _ => null,
        value =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        });

    /// <summary>
    /// BCS type for TransactionExpiration (enum: None, Epoch(u64), ValidDuring).
    /// </summary>
    public static BcsType<TransactionExpirationValue> TransactionExpiration { get; } = new BcsType<TransactionExpirationValue>(
        "TransactionExpiration",
        reader =>
        {
            ulong index = reader.ReadUleb128();
            return index switch
            {
                TransactionExpirationVariantNone => new TransactionExpirationNone(),
                TransactionExpirationVariantEpoch => new TransactionExpirationEpoch(reader.Read64()),
                TransactionExpirationVariantValidDuring => new TransactionExpirationValidDuring(ValidDuring.Read(reader)),
                _ => throw new InvalidOperationException($"Unknown TransactionExpiration variant: {index}.")
            };
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            switch (value)
            {
                case TransactionExpirationNone:
                    writer.WriteUleb128(TransactionExpirationVariantNone);
                    break;
                case TransactionExpirationEpoch epoch:
                    writer.WriteUleb128(TransactionExpirationVariantEpoch);
                    writer.WriteU64(epoch.Epoch);
                    break;
                case TransactionExpirationValidDuring validDuring:
                    writer.WriteUleb128(TransactionExpirationVariantValidDuring);
                    ValidDuring.Write(validDuring.Value, writer);
                    break;
                default:
                    throw new ArgumentException($"Unknown TransactionExpiration type: {value.GetType().Name}.", nameof(value));
            }
        },
        _ => null,
        value =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        });

    /// <summary>
    /// BCS type for TransactionKind (enum: ProgrammableTransaction only; other variants not serialized in typical flow).
    /// </summary>
    public static BcsType<TransactionKindValue> TransactionKind { get; } = new BcsType<TransactionKindValue>(
        "TransactionKind",
        reader =>
        {
            ulong index = reader.ReadUleb128();
            if (index != TransactionKindVariantProgrammableTransaction)
            {
                throw new InvalidOperationException(
                    $"Unsupported TransactionKind variant: {index}. Only ProgrammableTransaction (0) is supported.");
            }

            ProgrammableTransaction programmable = ProgrammableTransactionBcs.ProgrammableTransaction.Read(reader);
            return new TransactionKindProgrammable(programmable);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is TransactionKindProgrammable programmable)
            {
                writer.WriteUleb128(TransactionKindVariantProgrammableTransaction);
                ProgrammableTransactionBcs.ProgrammableTransaction.Write(programmable.Value, writer);
            }
            else
            {
                throw new ArgumentException(
                    $"Unsupported TransactionKind type: {value.GetType().Name}. Only ProgrammableTransaction is supported.",
                    nameof(value));
            }
        },
        _ => null,
        value =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        });

    /// <summary>
    /// BCS type for TransactionDataV1 (struct: kind, sender, gasData, expiration).
    /// </summary>
    public static BcsType<TransactionDataV1> TransactionDataV1 { get; } = new BcsType<TransactionDataV1>(
        "TransactionDataV1",
        reader =>
        {
            TransactionKindValue kind = TransactionKind.Read(reader);
            string sender = SuiBcsTypes.Address.Read(reader);
            GasData gasData = GasData.Read(reader);
            TransactionExpirationValue expiration = TransactionExpiration.Read(reader);
            return new TransactionDataV1(kind, sender, gasData, expiration);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            TransactionKind.Write(value.Kind, writer);
            SuiBcsTypes.Address.Write(value.Sender, writer);
            GasData.Write(value.GasData, writer);
            TransactionExpiration.Write(value.Expiration, writer);
        },
        _ => null,
        value =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        });

    /// <summary>
    /// BCS type for TransactionData (enum: V1). Use this to serialize transaction data for signing and submission.
    /// </summary>
    public static BcsType<TransactionData> TransactionData { get; } = new BcsType<TransactionData>(
        "TransactionData",
        reader =>
        {
            ulong index = reader.ReadUleb128();
            if (index != TransactionDataVariantV1)
            {
                throw new InvalidOperationException($"Unsupported TransactionData variant: {index}.");
            }

            TransactionDataV1 v1 = TransactionDataV1.Read(reader);
            return new TransactionData(v1);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            writer.WriteUleb128(TransactionDataVariantV1);
            TransactionDataV1.Write(value.V1, writer);
        },
        _ => null,
        value =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        });
}
