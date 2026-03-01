# Sui .NET SDK

.NET 10 SDK for the [Sui](https://sui.io) blockchain. Port of [Mysten Labs TypeScript SDK](https://github.com/MystenLabs/sui).

## Packages

| Package | Description |
|---------|-------------|
| **MystenLabs.Sui** | Main SDK: JSON-RPC client, transactions, signing, keypairs, SuiClient |
| **MystenLabs.Sui.Bcs** | BCS serialization, Move types, transaction BCS |
| **MystenLabs.Sui.Utils** | Hex, Base58, Base64, formatting utilities |
| **MystenLabs.Sui.Grpc** | gRPC client for Sui full node (State, Ledger, TransactionExecution) |
| **MystenLabs.Sui.GraphQL** | GraphQL client for Sui GraphQL RPC (indexer) |

## Install

```bash
dotnet add package MystenLabs.Sui
# Optional:
dotnet add package MystenLabs.Sui.Grpc
dotnet add package MystenLabs.Sui.GraphQL
```

## Quick example

```csharp
using MystenLabs.Sui;

var client = new SuiClient("https://fullnode.mainnet.sui.io");
var balance = await client.GetBalanceAsync("0xYourAddress");
var tx = new TransactionBuilder()
    .SetSender("0x...")
    .MoveCall("0x2::sui::transfer", ["0x2::sui::SUI"], [coin, recipient])
    .Build();
```

## License

Apache-2.0. See [Mysten Labs Sui](https://github.com/MystenLabs/sui).
