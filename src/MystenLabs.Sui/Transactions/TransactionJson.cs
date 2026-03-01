namespace MystenLabs.Sui.Transactions;

using System.Text.Json;
using MystenLabs.Sui.Bcs;

/// <summary>
/// Deserializes transaction data from JSON (same schema as TypeScript SDK: data.kind, data.sender, data.gasData, data.expiration).
/// </summary>
public static class TransactionJson
{
    private const string KeyData = "data";
    private const string KeyKind = "kind";
    private const string KeyProgrammableTransaction = "ProgrammableTransaction";
    private const string KeyInputs = "inputs";
    private const string KeyCommands = "commands";
    private const string KeySender = "sender";
    private const string KeyGasData = "gasData";
    private const string KeyExpiration = "expiration";
    private const string KeyPayment = "payment";
    private const string KeyOwner = "owner";
    private const string KeyPrice = "price";
    private const string KeyBudget = "budget";
    private const string KeyPure = "Pure";
    private const string KeyObject = "Object";
    private const string KeyBytes = "bytes";
    private const string KeyImmOrOwnedObject = "ImmOrOwnedObject";
    private const string KeySharedObjectRef = "SharedObjectRef";
    private const string KeyReceiving = "Receiving";
    private const string KeyMoveCall = "MoveCall";
    private const string KeyTransferObjects = "TransferObjects";
    private const string KeySplitCoins = "SplitCoins";
    private const string KeyMergeCoins = "MergeCoins";
    private const string KeyMakeMoveVec = "MakeMoveVec";
    private const string KeyPublish = "Publish";
    private const string KeyUpgrade = "Upgrade";
    private const string KeyPackage = "package";
    private const string KeyModule = "module";
    private const string KeyFunction = "function";
    private const string KeyTypeArguments = "typeArguments";
    private const string KeyArguments = "arguments";
    private const string KeyEpoch = "Epoch";
    private const string KeyGasCoin = "GasCoin";
    private const string KeyInput = "Input";
    private const string KeyResult = "Result";
    private const string KeyNestedResult = "NestedResult";

    /// <summary>
    /// Deserializes a transaction from JSON string. Expects root with optional "data" wrapper, then kind, sender, gasData, expiration.
    /// </summary>
    /// <param name="json">JSON string (e.g. from TS SDK transaction.toJSON()).</param>
    /// <returns>Transaction with parsed data.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="JsonException">If JSON structure is invalid or unsupported.</exception>
    public static Transaction FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentNullException(nameof(json));
        }

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement dataEl = root.TryGetProperty(KeyData, out JsonElement dataProp) ? dataProp : root;

        TransactionKindValue kind = ParseKind(dataEl.GetProperty(KeyKind));
        string sender = dataEl.TryGetProperty(KeySender, out JsonElement senderEl)
            ? senderEl.GetString() ?? ""
            : Transaction.PlaceholderAddress;
        GasData gasData = ParseGasData(dataEl.TryGetProperty(KeyGasData, out JsonElement gasEl) ? gasEl : default);
        TransactionExpirationValue expiration = ParseExpiration(
            dataEl.TryGetProperty(KeyExpiration, out JsonElement expEl) ? expEl : default);

        var v1 = new TransactionDataV1(kind, sender, gasData, expiration);
        return new Transaction(new TransactionData(v1));
    }

    private static TransactionKindValue ParseKind(JsonElement kindEl)
    {
        if (!kindEl.TryGetProperty(KeyProgrammableTransaction, out JsonElement ptEl))
        {
            throw new JsonException("Transaction kind must contain ProgrammableTransaction.");
        }

        CallArg[] inputs = ParseInputs(ptEl.GetProperty(KeyInputs));
        CommandValue[] commands = ParseCommands(ptEl.GetProperty(KeyCommands));
        var pt = new ProgrammableTransaction(inputs, commands);
        return new TransactionKindProgrammable(pt);
    }

    private static CallArg[] ParseInputs(JsonElement inputsEl)
    {
        if (inputsEl.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("inputs must be an array.");
        }

        int count = inputsEl.GetArrayLength();
        var result = new CallArg[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = ParseCallArg(inputsEl[index]);
        }

        return result;
    }

    private static CallArg ParseCallArg(JsonElement argEl)
    {
        if (argEl.TryGetProperty(KeyPure, out JsonElement pureEl))
        {
            string? base64 = pureEl.TryGetProperty(KeyBytes, out JsonElement bytesEl) ? bytesEl.GetString() : null;
            byte[] bytes = string.IsNullOrEmpty(base64) ? [] : Convert.FromBase64String(base64);
            return new CallArgPure(bytes);
        }

        if (argEl.TryGetProperty(KeyObject, out JsonElement objectEl))
        {
            if (objectEl.TryGetProperty(KeyImmOrOwnedObject, out JsonElement immEl))
            {
                string objectId = immEl.GetProperty("objectId").GetString() ?? "";
                ulong version = ParseVersion(immEl, "version");
                string digest = immEl.TryGetProperty("digest", out JsonElement dEl) ? dEl.GetString() ?? "" : "";
                var refObj = new SuiObjectRef(objectId, version, digest);
                return new CallArgObject(new ObjectArgImmOrOwned(refObj));
            }

            if (objectEl.TryGetProperty(KeySharedObjectRef, out JsonElement sharedEl))
            {
                string objectId = sharedEl.GetProperty("objectId").GetString() ?? "";
                ulong initialSharedVersion = ParseVersion(sharedEl, "initialSharedVersion");
                bool mutable = sharedEl.TryGetProperty("mutable", out JsonElement mutEl) && mutEl.GetBoolean();
                var sharedRef = new SharedObjectRef(objectId, initialSharedVersion, mutable);
                return new CallArgObject(new ObjectArgShared(sharedRef));
            }

            if (objectEl.TryGetProperty(KeyReceiving, out JsonElement recvEl))
            {
                string objectId = recvEl.GetProperty("objectId").GetString() ?? "";
                ulong version = ParseVersion(recvEl, "version");
                string digest = recvEl.TryGetProperty("digest", out JsonElement dEl) ? dEl.GetString() ?? "" : "";
                var refObj = new SuiObjectRef(objectId, version, digest);
                return new CallArgObject(new ObjectArgReceiving(refObj));
            }
        }

        throw new JsonException("Unsupported call arg shape. Expected Pure or Object(ImmOrOwnedObject|SharedObjectRef|Receiving).");
    }

    private static ulong ParseVersion(JsonElement el, string propertyName)
    {
        if (el.TryGetProperty(propertyName, out JsonElement vEl))
        {
            if (vEl.ValueKind == JsonValueKind.String)
            {
                return ulong.TryParse(vEl.GetString(), out ulong v) ? v : 0;
            }

            if (vEl.ValueKind == JsonValueKind.Number)
            {
                return vEl.GetUInt64();
            }
        }

        return 0;
    }

    private static CommandValue[] ParseCommands(JsonElement commandsEl)
    {
        if (commandsEl.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("commands must be an array.");
        }

        int count = commandsEl.GetArrayLength();
        var result = new CommandValue[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = ParseCommand(commandsEl[index]);
        }

        return result;
    }

    private static CommandValue ParseCommand(JsonElement cmdEl)
    {
        if (cmdEl.TryGetProperty(KeyMoveCall, out JsonElement mcEl))
        {
            string package = mcEl.GetProperty(KeyPackage).GetString() ?? "";
            string module = mcEl.GetProperty(KeyModule).GetString() ?? "";
            string function = mcEl.GetProperty(KeyFunction).GetString() ?? "";
            TypeTagValue[] typeArgs = mcEl.TryGetProperty(KeyTypeArguments, out JsonElement taEl)
                ? ParseTypeArguments(taEl)
                : [];
            ArgumentValue[] arguments = mcEl.TryGetProperty(KeyArguments, out JsonElement argsEl)
                ? ParseArguments(argsEl)
                : [];
            var moveCall = new ProgrammableMoveCall(package, module, function, typeArgs, arguments);
            return new CommandMoveCall(moveCall);
        }

        if (cmdEl.TryGetProperty(KeyTransferObjects, out JsonElement toEl))
        {
            ArgumentValue[] objects = ParseArguments(toEl.GetProperty("objects"));
            ArgumentValue address = ParseArgument(toEl.GetProperty("address"));
            return new CommandTransferObjects(objects, address);
        }

        if (cmdEl.TryGetProperty(KeySplitCoins, out JsonElement scEl))
        {
            ArgumentValue coin = ParseArgument(scEl.GetProperty("coin"));
            ArgumentValue[] amounts = ParseArguments(scEl.GetProperty("amounts"));
            return new CommandSplitCoins(coin, amounts);
        }

        if (cmdEl.TryGetProperty(KeyMergeCoins, out JsonElement mergeEl))
        {
            ArgumentValue destination = ParseArgument(mergeEl.GetProperty("destination"));
            ArgumentValue[] sources = ParseArguments(mergeEl.GetProperty("sources"));
            return new CommandMergeCoins(destination, sources);
        }

        if (cmdEl.TryGetProperty(KeyMakeMoveVec, out JsonElement mmvEl))
        {
            TypeTagValue? type = mmvEl.TryGetProperty("type", out JsonElement typeEl)
                ? ParseTypeTag(typeEl)
                : null;
            ArgumentValue[] elements = ParseArguments(mmvEl.GetProperty("elements"));
            return new CommandMakeMoveVec(type, elements);
        }

        if (cmdEl.TryGetProperty(KeyPublish, out JsonElement pubEl))
        {
            byte[][] modules = ParseModules(pubEl.GetProperty("modules"));
            string[] dependencies = ParseStringArray(pubEl.GetProperty("dependencies"));
            return new CommandPublish(modules, dependencies);
        }

        if (cmdEl.TryGetProperty(KeyUpgrade, out JsonElement upgEl))
        {
            byte[][] modules = ParseModules(upgEl.GetProperty("modules"));
            string[] dependencies = ParseStringArray(upgEl.GetProperty("dependencies"));
            string package = upgEl.GetProperty(KeyPackage).GetString() ?? "";
            ArgumentValue ticket = ParseArgument(upgEl.GetProperty("ticket"));
            return new CommandUpgrade(modules, dependencies, package, ticket);
        }

        throw new JsonException("Unsupported command type. Expected MoveCall, TransferObjects, SplitCoins, MergeCoins, MakeMoveVec, Publish, or Upgrade.");
    }

    private static TypeTagValue[] ParseTypeArguments(JsonElement taEl)
    {
        if (taEl.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        int count = taEl.GetArrayLength();
        var result = new TypeTagValue[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = ParseTypeTag(taEl[index]);
        }

        return result;
    }

    private static TypeTagValue ParseTypeTag(JsonElement typeEl)
    {
        if (typeEl.ValueKind == JsonValueKind.String)
        {
            string str = typeEl.GetString() ?? "";
            return TypeTagSerializer.ParseFromStr(str);
        }

        throw new JsonException("Type argument must be a string (e.g. 0x2::sui::SUI).");
    }

    private static ArgumentValue[] ParseArguments(JsonElement argsEl)
    {
        if (argsEl.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("arguments must be an array.");
        }

        int count = argsEl.GetArrayLength();
        var result = new ArgumentValue[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = ParseArgument(argsEl[index]);
        }

        return result;
    }

    private static ArgumentValue ParseArgument(JsonElement argEl)
    {
        if (argEl.TryGetProperty(KeyGasCoin, out _))
        {
            return new ArgumentGasCoin();
        }

        if (argEl.TryGetProperty(KeyInput, out JsonElement inputEl))
        {
            ushort index = (ushort)inputEl.GetUInt32();
            return new ArgumentInput(index);
        }

        if (argEl.TryGetProperty(KeyResult, out JsonElement resultEl))
        {
            ushort index = (ushort)resultEl.GetUInt32();
            return new ArgumentResult(index);
        }

        if (argEl.TryGetProperty(KeyNestedResult, out JsonElement nestedEl))
        {
            ushort commandIndex = (ushort)nestedEl.GetProperty("command").GetUInt32();
            ushort resultIndex = (ushort)nestedEl.GetProperty("result").GetUInt32();
            return new ArgumentNestedResult(commandIndex, resultIndex);
        }

        throw new JsonException("Unsupported argument. Expected GasCoin, Input, Result, or NestedResult.");
    }

    private static byte[][] ParseModules(JsonElement modulesEl)
    {
        if (modulesEl.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("modules must be an array.");
        }

        int count = modulesEl.GetArrayLength();
        var result = new byte[count][];
        for (int index = 0; index < count; index++)
        {
            string? base64 = modulesEl[index].GetString();
            result[index] = string.IsNullOrEmpty(base64) ? [] : Convert.FromBase64String(base64);
        }

        return result;
    }

    private static string[] ParseStringArray(JsonElement arrEl)
    {
        if (arrEl.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        int count = arrEl.GetArrayLength();
        var result = new string[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = arrEl[index].GetString() ?? "";
        }

        return result;
    }

    private static GasData ParseGasData(JsonElement gasEl)
    {
        if (gasEl.ValueKind == JsonValueKind.Null || gasEl.ValueKind == JsonValueKind.Undefined)
        {
            return new GasData([], Transaction.PlaceholderAddress, 0, 0);
        }

        SuiObjectRef[] payment = [];
        if (gasEl.TryGetProperty(KeyPayment, out JsonElement payEl) && payEl.ValueKind == JsonValueKind.Array)
        {
            int count = payEl.GetArrayLength();
            payment = new SuiObjectRef[count];
            for (int index = 0; index < count; index++)
            {
                JsonElement refEl = payEl[index];
                string objectId = refEl.GetProperty("objectId").GetString() ?? "";
                ulong version = ParseVersion(refEl, "version");
                string digest = refEl.TryGetProperty("digest", out JsonElement dEl) ? dEl.GetString() ?? "" : "";
                payment[index] = new SuiObjectRef(objectId, version, digest);
            }
        }

        string owner = gasEl.TryGetProperty(KeyOwner, out JsonElement ownerEl) ? ownerEl.GetString() ?? "" : Transaction.PlaceholderAddress;
        ulong price = gasEl.TryGetProperty(KeyPrice, out JsonElement priceEl) ? priceEl.GetUInt64() : 0;
        ulong budget = gasEl.TryGetProperty(KeyBudget, out JsonElement budgetEl) ? budgetEl.GetUInt64() : 0;
        return new GasData(payment, owner, price, budget);
    }

    private static TransactionExpirationValue ParseExpiration(JsonElement expEl)
    {
        if (expEl.ValueKind == JsonValueKind.Null || expEl.ValueKind == JsonValueKind.Undefined)
        {
            return new TransactionExpirationNone();
        }

        if (expEl.TryGetProperty(KeyEpoch, out JsonElement epochEl))
        {
            ulong epoch = epochEl.GetUInt64();
            return new TransactionExpirationEpoch(epoch);
        }

        if (expEl.ValueKind == JsonValueKind.Object && expEl.GetPropertyCount() == 0)
        {
            return new TransactionExpirationNone();
        }

        return new TransactionExpirationNone();
    }
}
