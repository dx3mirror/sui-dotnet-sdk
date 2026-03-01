namespace MystenLabs.Sui.Bcs;

/// <summary>
/// BCS serialization for <see cref="CommandValue"/> (enum: MoveCall, TransferObjects, SplitCoins, MergeCoins, Publish, MakeMoveVec, Upgrade).
/// </summary>
public static class CommandBcs
{
    private const int VariantMoveCall = 0;
    private const int VariantTransferObjects = 1;
    private const int VariantSplitCoins = 2;
    private const int VariantMergeCoins = 3;
    private const int VariantPublish = 4;
    private const int VariantMakeMoveVec = 5;
    private const int VariantUpgrade = 6;

    private static BcsType<TypeTagValue?> MakeMoveVecTypeOption { get; } = Bcs.OptionRef(TypeTagBcs.TypeTag);
    private static BcsType<byte[][]> ModulesVector { get; } = Bcs.Vector(Bcs.ByteVector());

    /// <summary>
    /// BCS type for Command.
    /// </summary>
    public static BcsType<CommandValue> Command { get; } = new BcsType<CommandValue>(
        "Command",
        reader =>
        {
            ulong index = reader.ReadUleb128();
            return index switch
            {
                VariantMoveCall => new CommandMoveCall(ProgrammableMoveCallBcs.ProgrammableMoveCall.Read(reader)),
                VariantTransferObjects => new CommandTransferObjects(
                    Bcs.Vector(ArgumentBcs.Argument).Read(reader),
                    ArgumentBcs.Argument.Read(reader)),
                VariantSplitCoins => new CommandSplitCoins(
                    ArgumentBcs.Argument.Read(reader),
                    Bcs.Vector(ArgumentBcs.Argument).Read(reader)),
                VariantMergeCoins => new CommandMergeCoins(
                    ArgumentBcs.Argument.Read(reader),
                    Bcs.Vector(ArgumentBcs.Argument).Read(reader)),
                VariantPublish => new CommandPublish(
                    ModulesVector.Read(reader),
                    Bcs.Vector(SuiBcsTypes.Address).Read(reader)),
                VariantMakeMoveVec => new CommandMakeMoveVec(
                    MakeMoveVecTypeOption.Read(reader),
                    Bcs.Vector(ArgumentBcs.Argument).Read(reader)),
                VariantUpgrade => new CommandUpgrade(
                    ModulesVector.Read(reader),
                    Bcs.Vector(SuiBcsTypes.Address).Read(reader),
                    SuiBcsTypes.Address.Read(reader),
                    ArgumentBcs.Argument.Read(reader)),
                _ => throw new InvalidOperationException($"Unknown Command variant index: {index}.")
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
                case CommandMoveCall moveCall:
                    writer.WriteUleb128(VariantMoveCall);
                    ProgrammableMoveCallBcs.ProgrammableMoveCall.Write(moveCall.MoveCall, writer);
                    break;
                case CommandTransferObjects transfer:
                    writer.WriteUleb128(VariantTransferObjects);
                    Bcs.Vector(ArgumentBcs.Argument).Write(transfer.Objects, writer);
                    ArgumentBcs.Argument.Write(transfer.Address, writer);
                    break;
                case CommandSplitCoins split:
                    writer.WriteUleb128(VariantSplitCoins);
                    ArgumentBcs.Argument.Write(split.Coin, writer);
                    Bcs.Vector(ArgumentBcs.Argument).Write(split.Amounts, writer);
                    break;
                case CommandMergeCoins merge:
                    writer.WriteUleb128(VariantMergeCoins);
                    ArgumentBcs.Argument.Write(merge.Destination, writer);
                    Bcs.Vector(ArgumentBcs.Argument).Write(merge.Sources, writer);
                    break;
                case CommandPublish publish:
                    writer.WriteUleb128(VariantPublish);
                    ModulesVector.Write(publish.Modules, writer);
                    Bcs.Vector(SuiBcsTypes.Address).Write(publish.Dependencies, writer);
                    break;
                case CommandMakeMoveVec makeMoveVec:
                    writer.WriteUleb128(VariantMakeMoveVec);
                    MakeMoveVecTypeOption.Write(makeMoveVec.Type, writer);
                    Bcs.Vector(ArgumentBcs.Argument).Write(makeMoveVec.Elements, writer);
                    break;
                case CommandUpgrade upgrade:
                    writer.WriteUleb128(VariantUpgrade);
                    ModulesVector.Write(upgrade.Modules, writer);
                    Bcs.Vector(SuiBcsTypes.Address).Write(upgrade.Dependencies, writer);
                    SuiBcsTypes.Address.Write(upgrade.Package, writer);
                    ArgumentBcs.Argument.Write(upgrade.Ticket, writer);
                    break;
                default:
                    throw new ArgumentException($"Unknown Command type: {value.GetType().Name}.", nameof(value));
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
}
