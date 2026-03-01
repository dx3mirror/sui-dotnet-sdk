# План переноса Sui TypeScript SDK в C# (.NET 10)

**Источник:** `C:\Users\Admin\Desktop\ts-sdks-main`  
**Референс:** [MystenLabs/ts-sdks](https://github.com/MystenLabs/ts-sdks)  
**Документация API:** https://sdk.mystenlabs.com/sui

---

## 1. Структура решения (D:\SuiDotnet)

```
SuiSdk.sln
├── src/
│   ├── MystenLabs.Sui.Utils/    ← @mysten/utils
│   ├── MystenLabs.Sui.Bcs/      ← @mysten/bcs
│   └── MystenLabs.Sui/          ← @mysten/sui (client, crypto, transactions, jsonRpc, …)
├── Directory.Packages.props     ← централизованные версии пакетов
├── Directory.Build.props        ← общие настройки (net10.0, nullable, …)
└── PORTING_PLAN.md              ← этот файл
```

**Централизованные пакеты (Directory.Packages.props):**
- `SimpleBase` — Base58 (замена @scure/base)
- `BouncyCastle.Cryptography` — Ed25519/ECDSA при необходимости (в .NET 9+ часть уже встроена)

---

## 2. Порядок переноса (фазы)

Переносить **снизу вверх** по зависимостям: Utils → Bcs → Sui (cryptography → bcs-типы → jsonRpc → transactions → client).

---

### Фаза 1: MystenLabs.Sui.Utils

**Соответствует:** `ts-sdks-main\packages\utils\`

| TS файл | Описание | C# класс/модуль |
|---------|----------|----------------|
| `src/hex.ts` | fromHex, toHex | `Utils/Hex.cs` ✅ (заглушка есть) |
| `src/b58.ts` | fromBase58, toBase58 (@scure/base) | `Utils/Base58.cs` — использовать SimpleBase |
| `src/b64.ts` | fromBase64, toBase64 | `Utils/Base64.cs` — обёртка над Convert |
| `src/chunk.ts` | chunk<T>(array, size) | `Utils/Chunk.cs` |
| `src/types.ts` | Simplify, UnionToIntersection | при необходимости как утилиты типов (в C# не 1:1) |
| `src/with-resolver.ts` | promiseWithResolvers | `Utils/TaskWithResolvers.cs` ✅ (TaskWithResolvers&lt;T&gt;.Create) |
| `src/dataloader.ts` | DataLoader | опционально, для batch RPC |
| `src/mitt.ts` | mitt (event emitter) | опционально |

**Референс:** `C:\Users\Admin\Desktop\ts-sdks-main\packages\utils\src\`

---

### Фаза 2: MystenLabs.Sui.Bcs

**Соответствует:** `ts-sdks-main\packages\bcs\`

| TS файл | Описание | C# |
|---------|----------|-----|
| `src/uleb.ts` | ULEB128 encode/decode | `Bcs/Uleb128.cs` |
| `src/writer.ts` | BCS writer (u8, u32, u64, bytes, string) | `Bcs/BcsWriter.cs` ✅ (базовая заглушка) |
| `src/reader.ts` | BCS reader | `Bcs/BcsReader.cs` |
| `src/types.ts` | примитивные типы BCS | `Bcs/Types.cs` |
| `src/bcs-type.ts` | BcsType, регистрация типов | `Bcs/BcsType.cs` |
| `src/bcs.ts` | BCS singleton, registerType, serialize, deserialize | `Bcs/Bcs.cs` |
| `src/utils.ts` | хелперы | `Bcs/Utils.cs` при необходимости |

**Референс:** `C:\Users\Admin\Desktop\ts-sdks-main\packages\bcs\src\`

**Зависимости:** только MystenLabs.Sui.Utils (hex/base58 для строковых представлений при необходимости).

---

### Фаза 3: MystenLabs.Sui — Cryptography и Keypairs

**Соответствует:** `ts-sdks-main\packages\sui\src\cryptography\` и `keypairs\`

| TS путь | Описание | C# |
|---------|----------|-----|
| `cryptography/signature-scheme.ts` | SignatureScheme enum (Ed25519, Secp256k1, …) | `Cryptography/SignatureScheme.cs` |
| `cryptography/publickey.ts` | PublicKey base, toBytes, toBase64, verify | `Cryptography/PublicKey.cs` |
| `cryptography/keypair.ts` | Keypair base (getPublicKey, sign) | `Cryptography/Keypair.cs` |
| `cryptography/intent.ts` | Intent scope, message intent | `Cryptography/Intent.cs` |
| `cryptography/mnemonics.ts` | BIP39 mnemonic ( @scure/bip39 ) | `Cryptography/Mnemonics.cs` ✅ (PBKDF2-HMAC-SHA512, path validation) |
| `keypairs/ed25519/publickey.ts` | Ed25519PublicKey | `Keypairs/Ed25519/Ed25519PublicKey.cs` |
| `keypairs/ed25519/keypair.ts` | Ed25519Keypair | `Keypairs/Ed25519/Ed25519Keypair.cs` — BouncyCastle; DeriveKeypair/DeriveKeypairFromSeed ✅ |
| `keypairs/ed25519/ed25519-hd-key.ts` | HD derivation | `Keypairs/Ed25519/Ed25519HdKey.cs` ✅ (SLIP-0010, HMAC-SHA512) |
| `keypairs/secp256k1/*` | Secp256k1 keypair/publickey | `Keypairs/Secp256k1/` — BouncyCastle; DeriveKeypair(mnemonic, path) ✅ |
| `keypairs/secp256r1/*` | Secp256r1 (P-256) | `Keypairs/Secp256r1/` — .NET ECDsa + BouncyCastle; DeriveKeypair(mnemonic, path) ✅ |
| BIP-32 HD (for Secp256k1/r1) | HDKey.fromMasterSeed, derive(path) | `Keypairs/Bip32.cs` ✅ (HMAC-SHA512, Secp256k1 curve) |
| `keypairs/passkey/*` | WebAuthn/Passkey | можно отложить (платформенно специфично) |

**Референс:**  
`C:\Users\Admin\Desktop\ts-sdks-main\packages\sui\src\cryptography\`  
`C:\Users\Admin\Desktop\ts-sdks-main\packages\sui\src\keypairs\ed25519\`  
`C:\Users\Admin\Desktop\ts-sdks-main\packages\sui\src\keypairs\secp256k1\`  
`C:\Users\Admin\Desktop\ts-sdks-main\packages\sui\src\keypairs\secp256r1\`

---

### Фаза 4: MystenLabs.Sui — Sui BCS типы

**Соответствует:** `ts-sdks-main\packages\sui\src\bcs\`

| TS | Описание | C# |
|----|----------|-----|
| `bcs/types.ts` | Address, ObjectId, StructTag, TypeTag, Transaction, ProgrammableTransaction, и т.д. | `Bcs/` в проекте MystenLabs.Sui (или отдельная папка SuiBcs) |

Регистрация Sui-типов в BCS и сериализация/десериализация транзакций и аргументов.

**Референс:** `C:\Users\Admin\Desktop\ts-sdks-main\packages\sui\src\bcs\`

---

### Фаза 5: MystenLabs.Sui — JsonRpc

**Соответствует:** `ts-sdks-main\packages\sui\src\jsonRpc\`

| TS файл | Описание | C# |
|---------|----------|-----|
| `jsonRpc/http-transport.ts` | HTTP transport для RPC | `JsonRpc/HttpTransport.cs` |
| `jsonRpc/client.ts` | RPC методы (sui_*): getObject, getOwnedObjects, getBalance, executeTransactionBlock, … | `JsonRpc/SuiRpcClient.cs` |
| `jsonRpc/types/` | Типы ответов RPC | `JsonRpc/Models/` или `Types/` |
| `jsonRpc/errors.ts` | SuiRpcError и т.д. | `JsonRpc/SuiRpcException.cs` |
| `jsonRpc/network.ts` | Константы сетей (mainnet, testnet, devnet) | `JsonRpc/Network.cs` |
| `jsonRpc/core.ts` | Ядро запроса (request id, JSON body) | `JsonRpc/RpcCore.cs` |

**Референс:** `C:\Users\Admin\Desktop\ts-sdks-main\packages\sui\src\jsonRpc\`

После этой фазы можно вызывать Sui RPC из C# (getObject, getBalance и т.д.) и подключать к `SuiClient`.

---

### Фаза 6: MystenLabs.Sui — Transactions

**Соответствует:** `ts-sdks-main\packages\sui\src\transactions\`

| TS | Описание | C# |
|----|----------|-----|
| `transactions/Commands.ts` | MoveCall, TransferObjects, SplitCoins, MergeCoins, … | `Transactions/Commands.cs` |
| `transactions/Arguments.ts` | Pure, Object, GasCoin, Result | `Transactions/Arguments.cs` |
| `transactions/Inputs.ts` | Типы входов для транзакций | `Transactions/Inputs.cs` |
| `transactions/data/v1.ts`, `v2.ts`, `internal.ts` | TransactionData (v1/v2), ProgrammableTransaction | `Transactions/Data/` |
| `transactions/hash.ts` | Хеширование транзакций для подписи | `Transactions/TransactionHasher.cs` |
| `transactions/object.ts` | ObjectRef, SharedObjectRef | `Transactions/ObjectRef.cs` |
| `transactions/ObjectCache.ts` | Кэш объектов при построении TX | `Transactions/ObjectCache.cs` |
| `transactions/intents/*` | Intent, CoinWithBalance и т.д. | `Transactions/Intents/` |
| `transactions/index.ts` | TransactionBuilder, signTransactionBlock, … | `Transactions/TransactionBuilder.cs`, `Transactions/Signer.cs` |

**Референс:** `C:\Users\Admin\Desktop\ts-sdks-main\packages\sui\src\transactions\`

---

### Фаза 7: MystenLabs.Sui — Client

**Соответствует:** `ts-sdks-main\packages\sui\src\client\`

| TS файл | Описание | C# |
|---------|----------|-----|
| `client/client.ts` | SuiClient: getObject, getBalance, signAndExecuteTransactionBlock, … | `SuiClient.cs` ✅ (расширить) |
| `client/cache.ts` | Кэш для объектов/балансов | `Client/Cache.cs` при необходимости |
| `client/core-resolver.ts` | Резолвер RPC/transport | `Client/CoreResolver.cs` при необходимости |

**Референс:** `C:\Users\Admin\Desktop\ts-sdks-main\packages\sui\src\client\`

После фазы 7 минимальный «полный» цикл: создать клиент → получить объект/баланс → построить транзакцию → подписать → отправить.

---

### Фаза 8 (опционально): Faucet, Multisig, Verify, ZkLogin

| Модуль TS | Путь в репо | Приоритет |
|-----------|-------------|-----------|
| Faucet | `packages/sui/src/faucet/` | средний (для devnet/testnet) |
| Multisig | `packages/sui/src/multisig/` | средний |
| Verify | `packages/sui/src/verify/` | средний (верификация подписей) |
| ZkLogin | `packages/sui/src/zklogin/` | низкий (сложно, зависимости) |
| GraphQL | `packages/sui/src/graphql/` | низкий |
| gRPC | `packages/sui/src/grpc/` | низкий (альтернативный транспорт) |

---

## 3. Карта путей: ts-sdks-main → SuiDotnet

| Источник (ts-sdks-main) | Назначение (SuiDotnet) |
|-------------------------|-------------------------|
| `packages/utils/src/*` | `src/MystenLabs.Sui.Utils/*.cs` |
| `packages/bcs/src/*` | `src/MystenLabs.Sui.Bcs/*.cs` |
| `packages/sui/src/cryptography/*` | `src/MystenLabs.Sui/Cryptography/*.cs` |
| `packages/sui/src/keypairs/*` | `src/MystenLabs.Sui/Keypairs/*.cs` |
| `packages/sui/src/bcs/*` | `src/MystenLabs.Sui/Bcs/` (Sui-типы BCS) |
| `packages/sui/src/jsonRpc/*` | `src/MystenLabs.Sui/JsonRpc/*.cs` |
| `packages/sui/src/transactions/*` | `src/MystenLabs.Sui/Transactions/*.cs` |
| `packages/sui/src/client/*` | `src/MystenLabs.Sui/Client/*.cs` + `SuiClient.cs` |
| `packages/sui/src/faucet/*` | `src/MystenLabs.Sui/Faucet/*.cs` |
| `packages/sui/src/verify/*` | `src/MystenLabs.Sui/Verify/*.cs` |
| `packages/sui/src/multisig/*` | `src/MystenLabs.Sui/Multisig/*.cs` |

---

## 4. Сборка и тесты

- Сборка: из корня `D:\SuiDotnet` выполнять `dotnet build`.
- После каждой фазы: `dotnet build` и по возможности unit-тесты (порт тестов из `packages/*/test` или написать свои).
- Рекомендуется завести тестовый проект `tests/MystenLabs.Sui.Tests` и подключать к нему примеры вызовов RPC (devnet) и BCS encode/decode.

---

## 5. Чек-лист по фазам

- [x] **Фаза 1** — Utils: Hex (с odd-length padding), Base58, Base64, Chunk; сборка Utils.
- [x] **Фаза 2** — Bcs: ULEB128, BcsWriter, BcsReader, BcsEncoding, BcsType, Bcs (u8–u256, bool, string, vector, option, tuple), SerializedBcs, BcsEncodeDecode; сборка Bcs.
- [x] **Фаза 3** — Cryptography + Keypairs: SignatureScheme, Intent, PublicKey, Signer, Keypair, Ed25519 (BouncyCastle), Bech32/SuiPrivateKeyEncoding; сборка Sui. (Secp256k1/Secp256r1 — опционально позже.)
- [x] **Фаза 4** — Sui BCS типы: Address, ObjectId (SuiBcsTypes); ObjectDigest, SuiObjectRef, SharedObjectRef, ObjectArg; StructTag, TypeTag, TypeTagSerializer; CallArg (Pure, Object, FundsWithdrawal); Reservation, WithdrawalType, WithdrawFrom, FundsWithdrawal.
- [x] **Фаза 5** — JsonRpc: HttpTransport, SuiRpcClient (getObject, getBalance, executeTransactionBlock, getReferenceGasPrice), Network, ошибки.
- [x] **Фаза 6** — Transactions: TransactionHasher; Argument, Command, ProgrammableMoveCall, ProgrammableTransaction (BCS); GasData, TransactionExpiration, ValidDuring, TransactionDataV1, TransactionData (BCS); TransactionDataBuilder (Build, BuildAndSerialize, SerializeToBcs).
- [x] **Фаза 7** — SuiClient (GetObject, GetBalance, ExecuteTransactionBlock, SignAndExecuteTransactionBlock); тестовый проект tests/MystenLabs.Sui.Tests, unit-тесты Utils/BCS.
- [x] **Фаза 8 (Faucet)** — FaucetClient (GetFaucetHost, RequestSuiFromFaucetV2Async), FaucetRateLimitException, FaucetCoinInfo/FaucetRequestSuiResult; SuiClient.RequestSuiFromFaucetAsync.
- [x] **Фаза 8 (Verify)** — SuiVerify (PublicKeyFromRawBytes, PublicKeyFromSuiBytes, VerifySignatureAsync, VerifyTransactionSignatureAsync, VerifyPersonalMessageSignatureAsync); Ed25519 only.
- [x] **Фаза 8 (Multisig)** — MultiSigBcs, MultiSigPublicKey (FromPublicKeys, VerifyAsync, CombinePartialSignatures), MultiSigSigner, MultiSigSignature.ParseSerialized; SuiVerify supports MultiSig.
- [ ] **Фаза 8 (остальное)** — ZkLogin, Passkey, GraphQL, gRPC (по необходимости).

---

*Документ можно обновлять по мере продвижения переноса (отмечать выполненные пункты и добавлять замечания).*
