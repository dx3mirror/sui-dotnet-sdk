# Sui .NET SDK

[![NuGet](https://img.shields.io/nuget/v/SuiDotnet.svg?style=flat-square)](https://www.nuget.org/packages/SuiDotnet)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

.NET SDK for the [Sui](https://sui.io) blockchain: RPC and gRPC clients, transaction building and signing, key management (Ed25519, Secp256k1, Secp256r1, multisig), and BIP32/BIP39.

**Requirements:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

---

## Installation

```bash
dotnet add package SuiDotnet
```

Or in `.csproj`:

```xml
<PackageReference Include="SuiDotnet" Version="0.1.0" />
```

---

## 1. Connect and read data

Create a client and call RPC:

```csharp
using MystenLabs.Sui;
using MystenLabs.Sui.JsonRpc;

var client = new SuiClient(SuiNetwork.Mainnet);

// Balance for an address (SUI by default)
var balance = await client.GetBalanceAsync("0xYourAddress");
Console.WriteLine($"Balance: {balance.TotalBalance} MIST");

// Get object by ID
var obj = await client.GetObjectAsync("0xObjectId");

// Owned objects (with optional filter and pagination)
var owned = await client.GetOwnedObjectsAsync("0xYourAddress");

// Reference gas price (needed when building transactions)
string gasPrice = await client.GetReferenceGasPriceAsync();
```

**Networks:** use `SuiNetwork.Mainnet`, `SuiNetwork.Testnet`, `SuiNetwork.Devnet`, or `SuiNetwork.Localnet`.

---

## 2. Keys: create and use

### From mnemonic (BIP39)

```csharp
using MystenLabs.Sui.Keypairs.Ed25519;

// Default path for Sui: m/44'/784'/0'/0'/0'
var keypair = Ed25519Keypair.DeriveKeypair(
    "your twelve or twenty four word mnemonic phrase here",
    path: "m/44'/784'/0'/0'/0'");

string address = keypair.PublicKey.ToSuiAddress();
// Use keypair as Signer when signing transactions
```

### Generate new keypair

```csharp
using MystenLabs.Sui.Keypairs.Ed25519;

var keypair = Ed25519Keypair.Generate();
string address = keypair.PublicKey.ToSuiAddress();
// Export/import via keypair.GetSecretKey() / FromSecretKey(...)
```

### Other schemes

- **Secp256k1:** `Secp256k1Keypair.DeriveKeypair(mnemonic, path)` or `Secp256k1Keypair.Generate()`
- **Secp256r1 (P-256):** `Secp256r1Keypair.DeriveKeypair(mnemonic, path)` or `Secp256r1Keypair.Generate()`

---

## 3. Build and send a transaction

Minimal example: **transfer SUI** (one gas coin to a recipient).

```csharp
using MystenLabs.Sui;
using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.JsonRpc;
using MystenLabs.Sui.Keypairs.Ed25519;
using MystenLabs.Sui.Transactions;

var client = new SuiClient(SuiNetwork.Devnet);

// 1) Keypair (in production load from env/secure storage)
var keypair = Ed25519Keypair.DeriveKeypair("your mnemonic...", "m/44'/784'/0'/0'/0'");
string sender = keypair.PublicKey.ToSuiAddress();

// 2) Gas: price from chain, budget in MIST (e.g. 100_000_000 = 0.1 SUI)
string gasPriceStr = await client.GetReferenceGasPriceAsync();
ulong gasPrice = ulong.Parse(gasPriceStr);
ulong gasBudget = 100_000_000;

// GasData: payment = gas coins (empty = use first owned coin on some networks), owner = sender
var gasData = new GasData(
    Payment: [],
    Owner: sender,
    Price: gasPrice,
    Budget: gasBudget);

// 3) Build: MoveCall 0x2::sui::transfer(coint, recipient)
var builder = new TransactionBuilder();
ArgumentValue recipient = builder.PureAddress("0xRecipientAddress");
builder
    .MoveCall("0x2::sui::transfer", [TransactionBuilder.Gas, recipient])
    .SetSender(sender)
    .SetGasData(gasData)
    .SetExpirationNone();

TransactionData txData = builder.Build();
var transaction = new Transaction(txData);

// 4) Sign and execute
SuiTransactionBlockResponse result = await client.SignAndExecuteTransactionBlockAsync(
    transaction,
    keypair,
    new { options = new { showEffects = true } });

Console.WriteLine($"Digest: {result.Digest}");
```

**Note:** `GasData` with empty `Payment` relies on the node to pick a gas coin for the sender. For production you typically select specific gas coins (e.g. from `GetCoinsAsync`) and pass them as `Payment` (array of `SuiObjectRef`).

---

## 4. More transaction patterns

### Split coins and transfer one part

```csharp
var builder = new TransactionBuilder();
ArgumentValue amount = builder.Pure(1_000_000u); // 0.001 SUI in MIST
ArgumentValue splitResult = builder.SplitCoins(TransactionBuilder.Gas, [amount]);
ArgumentValue recipient = builder.PureAddress("0xRecipientAddress");
builder.TransferObjects([splitResult], recipient);
builder.SetSender(sender).SetGasData(gasData).SetExpirationNone();

var transaction = new Transaction(builder.Build());
var result = await client.SignAndExecuteTransactionBlockAsync(transaction, keypair);
```

### Move call with type arguments

```csharp
// e.g. call a function that takes a type parameter
builder.MoveCall(
    "0xPackageId::module_name::function_name",
    [builder.Gas, builder.PureAddress("0x...")],
    typeArguments: ["0x2::sui::SUI"]);
```

### Object by ID (when you have object ref)

```csharp
// If you have objectId, version, digest (e.g. from GetObjectAsync):
ArgumentValue objArg = builder.ObjectRef(objectId, version, digest);
builder.TransferObjects([objArg], builder.PureAddress(recipientAddress));
```

### Unresolved object ID (resolve at build time)

If you use `builder.Object("0xObjectId")` you must resolve it before building. Add the resolution plugin and call `PrepareForSerializationAsync` with a client and optional object cache:

```csharp
builder.AddBuildPlugin(TransactionResolvingHelpers.ResolveTransactionPlugin);
await builder.PrepareForSerializationAsync(new BuildTransactionOptions { Client = client });
TransactionData txData = builder.Build();
```

---

## 5. Faucet (devnet / testnet)

```csharp
using MystenLabs.Sui.Faucet;

// Use constants: FaucetClient.HostDevnet, HostTestnet, or HostLocalnet
var result = await FaucetClient.RequestSuiFromFaucetV2Async(FaucetClient.HostDevnet, "0xYourAddress");
// result.CoinsSent; throws FaucetRateLimitException on HTTP 429
```

---

## 6. gRPC client

For lower-level or high-throughput use, Sui gRPC is available:

```csharp
using MystenLabs.Sui.Grpc;

var grpc = new SuiGrpcClient(SuiGrpcClient.DefaultMainnetAddress);

// State: objects, balances, dynamic fields
var state = grpc.State;
var objResponse = await state.GetObjectAsync(new GetObjectRequest { ObjectId = "0x..." });

// Ledger: checkpoints, transaction blocks
var ledger = grpc.Ledger;

// Execute transaction
var exec = grpc.TransactionExecution;
```

---

## 7. GraphQL client

Sui GraphQL indexer (beta):

```csharp
using MystenLabs.Sui.GraphQL;

var graphql = new SuiGraphQLClient(SuiGraphQLClient.DefaultMainnetEndpoint);
var response = await graphql.ExecuteAsync(
    "query { object(address: $address) { ... } }",
    new { address = "0x..." });
```

---

## Project structure

| Project | Description |
|--------|-------------|
| **MystenLabs.Sui** | Main SDK: `SuiClient`, transactions, cryptography, multisig, verification, RPC models |
| **MystenLabs.Sui.Bcs** | BCS serialization (reader/writer, types) |
| **MystenLabs.Sui.Utils** | Base58, Base64, Hex, formatting |
| **MystenLabs.Sui.Grpc** | Sui gRPC client (State, Ledger, Transaction Execution) |
| **MystenLabs.Sui.GraphQL** | Sui GraphQL client |

---

## Supported methods

### High-level client (`SuiClient`)

All methods are async. Underlying JSON-RPC method names are exposed as static constants in **`MystenLabs.Sui.JsonRpc.RpcMethods`** (e.g. `RpcMethods.GetObject`, `RpcMethods.ExecuteTransactionBlock`).

| Method | RPC constant |
|--------|--------------|
| `GetObjectAsync` | `RpcMethods.GetObject` |
| `GetBalanceAsync` | `RpcMethods.GetBalance` |
| `GetReferenceGasPriceAsync` | `RpcMethods.GetReferenceGasPrice` |
| `GetOwnedObjectsAsync` | `RpcMethods.GetOwnedObjects` |
| `MultiGetObjectsAsync` | `RpcMethods.MultiGetObjects` |
| `GetTransactionBlockAsync` | `RpcMethods.GetTransactionBlock` |
| `GetAllBalancesAsync` | `RpcMethods.GetAllBalances` |
| `GetCoinsAsync` | `RpcMethods.GetCoins` |
| `GetAllCoinsAsync` | `RpcMethods.GetAllCoins` |
| `QueryTransactionBlocksAsync` | `RpcMethods.QueryTransactionBlocks` |
| `WaitForTransactionBlockAsync` | `RpcMethods.WaitForTransactionBlock` |
| `ExecuteTransactionBlockAsync` | `RpcMethods.ExecuteTransactionBlock` |
| `SignAndExecuteTransactionBlockAsync` | (signs + execute) |
| `GetCoinMetadataAsync` | `RpcMethods.GetCoinMetadata` |
| `TryGetPastObjectAsync` | `RpcMethods.TryGetPastObject` |
| `MultiGetTransactionBlocksAsync` | `RpcMethods.MultiGetTransactionBlocks` |
| `GetTotalSupplyAsync` | `RpcMethods.GetTotalSupply` |
| `DevInspectTransactionBlockAsync` | `RpcMethods.DevInspectTransactionBlock` |
| `DryRunTransactionBlockAsync` | `RpcMethods.DryRunTransactionBlock` |
| `GetCheckpointAsync` | `RpcMethods.GetCheckpoint` |
| `GetLatestCheckpointSequenceNumberAsync` | `RpcMethods.GetLatestCheckpointSequenceNumber` |
| `GetChainIdentifierAsync` | `RpcMethods.GetChainIdentifier` |
| `GetStakesAsync` | `RpcMethods.GetStakes` |
| `GetStakesByIdsAsync` | `RpcMethods.GetStakesByIds` |
| `GetCheckpointsAsync` | `RpcMethods.GetCheckpoints` |
| `QueryEventsAsync` | `RpcMethods.QueryEvents` |
| `GetLatestSuiSystemStateAsync` | `RpcMethods.GetLatestSuiSystemState` |
| `GetCurrentEpochAsync` | `RpcMethods.GetCurrentEpoch` |
| `GetValidatorsApyAsync` | `RpcMethods.GetValidatorsApy` |
| `GetDynamicFieldsAsync` | `RpcMethods.GetDynamicFields` |
| `GetDynamicFieldObjectAsync` | `RpcMethods.GetDynamicFieldObject` |
| `GetCommitteeInfoAsync` | `RpcMethods.GetCommitteeInfo` |
| `GetNetworkMetricsAsync` | `RpcMethods.GetNetworkMetrics` |
| `GetAddressMetricsAsync` | `RpcMethods.GetLatestAddressMetrics` |
| `GetEpochMetricsAsync` | `RpcMethods.GetEpochMetrics` |
| `GetProtocolConfigAsync` | `RpcMethods.GetProtocolConfig` |
| `GetMoveCallMetricsAsync` | `RpcMethods.GetMoveCallMetrics` |
| `GetRpcApiVersionAsync` | `RpcMethods.RpcDiscover` |
| `GetTotalTransactionBlocksAsync` | `RpcMethods.GetTotalTransactionBlocks` |
| `ResolveNameServiceAddressAsync` | `RpcMethods.ResolveNameServiceAddress` |
| `ResolveNameServiceNamesAsync` | `RpcMethods.ResolveNameServiceNames` |
| `GetAllEpochAddressMetricsAsync` | `RpcMethods.GetAllEpochAddressMetrics` |
| `GetMoveFunctionArgTypesAsync` | `RpcMethods.GetMoveFunctionArgTypes` |
| `GetNormalizedMoveModulesByPackageAsync` | `RpcMethods.GetNormalizedMoveModulesByPackage` |
| `GetNormalizedMoveModuleAsync` | `RpcMethods.GetNormalizedMoveModule` |
| `GetNormalizedMoveFunctionAsync` | `RpcMethods.GetNormalizedMoveFunction` |
| `GetNormalizedMoveStructAsync` | `RpcMethods.GetNormalizedMoveStruct` |
| `RequestSuiFromFaucetAsync` | (faucet helper) |

### gRPC client (`SuiGrpcClient`)

Access via **`grpc.State`**, **`grpc.Ledger`**, **`grpc.TransactionExecution`**. Each service exposes sync and async methods (e.g. `GetObject` / `GetObjectAsync`).

| Service | Methods |
|---------|---------|
| **State** (`grpc.State`) | `ListDynamicFields`, `ListOwnedObjects`, `GetCoinInfo`, `GetBalance`, `ListBalances` |
| **Ledger** (`grpc.Ledger`) | `GetServiceInfo`, `GetObject`, `BatchGetObjects`, `GetTransaction`, `BatchGetTransactions`, `GetCheckpoint`, `GetEpoch` |
| **TransactionExecution** (`grpc.TransactionExecution`) | `ExecuteTransaction`, `SimulateTransaction` |

---

## Supported key schemes

- **Ed25519** — default for many Sui wallets; BIP32 path `m/44'/784'/0'/0'/0'`
- **Secp256k1** — Ethereum-style; compatible with MetaMask-style keys
- **Secp256r1 (P-256)** — NIST P-256; hardware/WebAuthn-friendly
- **Multisig** — threshold multi-signature (e.g. 2-of-3)

---

## License

Apache-2.0. See [LICENSE](LICENSE) for details.

---

## Links

- [Sui Documentation](https://docs.sui.io)
- [NuGet package](https://www.nuget.org/packages/SuiDotnet)
- [Repository](https://github.com/dx3mirror/sui-dotnet-sdk)
