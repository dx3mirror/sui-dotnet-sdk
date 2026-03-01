namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Reservation for funds withdrawal (max amount as u64).
/// </summary>
public sealed record Reservation(ulong MaxAmountU64);

/// <summary>
/// Withdrawal type: balance with an associated type tag.
/// </summary>
public sealed record WithdrawalType(TypeTagValue Balance);

/// <summary>
/// Withdraw from sender or sponsor.
/// </summary>
public abstract record WithdrawFrom;

/// <summary>
/// Withdraw from sender.
/// </summary>
public sealed record WithdrawFromSender : WithdrawFrom;

/// <summary>
/// Withdraw from sponsor.
/// </summary>
public sealed record WithdrawFromSponsor : WithdrawFrom;

/// <summary>
/// Funds withdrawal argument: reservation, type, and source (sender/sponsor).
/// </summary>
/// <param name="Reservation">Reservation (e.g. max amount).</param>
/// <param name="TypeArg">Withdrawal type (balance type tag).</param>
/// <param name="WithdrawFrom">Sender or sponsor.</param>
public sealed record FundsWithdrawal(Reservation Reservation, WithdrawalType TypeArg, WithdrawFrom WithdrawFrom);

/// <summary>
/// BCS serialization for Reservation, WithdrawalType, WithdrawFrom, and FundsWithdrawal.
/// </summary>
public static class FundsWithdrawalBcs
{
    private const int ReservationVariantMaxAmountU64 = 0;
    private const int WithdrawalTypeVariantBalance = 0;
    private const int WithdrawFromVariantSender = 0;
    private const int WithdrawFromVariantSponsor = 1;

    /// <summary>
    /// BCS type for Reservation (enum: MaxAmountU64(u64)).
    /// </summary>
    public static BcsType<Reservation> Reservation { get; } = new BcsType<Reservation>(
        "Reservation",
        reader =>
        {
            ulong index = reader.ReadUleb128();
            if (index != ReservationVariantMaxAmountU64)
            {
                throw new InvalidOperationException($"Unknown Reservation variant: {index}.");
            }

            ulong maxAmount = reader.Read64();
            return new Reservation(maxAmount);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            writer.WriteUleb128(ReservationVariantMaxAmountU64);
            writer.WriteU64(value.MaxAmountU64);
        },
        value => value != null ? 1 + 8 : null,
        value =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        });

    /// <summary>
    /// BCS type for WithdrawalType (enum: Balance(TypeTag)).
    /// </summary>
    public static BcsType<WithdrawalType> WithdrawalType { get; } = new BcsType<WithdrawalType>(
        "WithdrawalType",
        reader =>
        {
            ulong index = reader.ReadUleb128();
            if (index != WithdrawalTypeVariantBalance)
            {
                throw new InvalidOperationException($"Unknown WithdrawalType variant: {index}.");
            }

            TypeTagValue balance = TypeTagBcs.TypeTag.Read(reader);
            return new WithdrawalType(balance);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            writer.WriteUleb128(WithdrawalTypeVariantBalance);
            TypeTagBcs.TypeTag.Write(value.Balance, writer);
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
    /// BCS type for WithdrawFrom (enum: Sender, Sponsor).
    /// </summary>
    public static BcsType<WithdrawFrom> WithdrawFrom { get; } = new BcsType<WithdrawFrom>(
        "WithdrawFrom",
        reader =>
        {
            ulong index = reader.ReadUleb128();
            return index switch
            {
                WithdrawFromVariantSender => new WithdrawFromSender(),
                WithdrawFromVariantSponsor => new WithdrawFromSponsor(),
                _ => throw new InvalidOperationException($"Unknown WithdrawFrom variant: {index}.")
            };
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int variant = value switch
            {
                WithdrawFromSender => WithdrawFromVariantSender,
                WithdrawFromSponsor => WithdrawFromVariantSponsor,
                _ => throw new ArgumentException($"Unknown WithdrawFrom type: {value.GetType().Name}.", nameof(value))
            };

            writer.WriteUleb128((ulong)variant);
        },
        value => value != null ? 1 : null,
        value =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        });

    /// <summary>
    /// BCS type for FundsWithdrawal (struct: reservation, typeArg, withdrawFrom).
    /// </summary>
    public static BcsType<FundsWithdrawal> FundsWithdrawal { get; } = new BcsType<FundsWithdrawal>(
        "FundsWithdrawal",
        reader =>
        {
            Reservation reservation = Reservation.Read(reader);
            WithdrawalType typeArg = WithdrawalType.Read(reader);
            WithdrawFrom withdrawFrom = WithdrawFrom.Read(reader);
            return new FundsWithdrawal(reservation, typeArg, withdrawFrom);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Reservation.Write(value.Reservation, writer);
            WithdrawalType.Write(value.TypeArg, writer);
            WithdrawFrom.Write(value.WithdrawFrom, writer);
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
