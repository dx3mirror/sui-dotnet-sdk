# Test coverage summary

## Covered by tests

| Area | Files / logic | Test file(s) |
|------|----------------|--------------|
| **Utils** | Hex, Base58, Base64, Chunk | HexTests, Base58Tests, Base64Tests, ChunkTests |
| **Bcs (core)** | Uleb128, BcsWriter/Reader, BcsType, Bool, U8–U64, Option, Vector, Tuple, EncodeStr/DecodeStr | Uleb128Tests, BcsWriterReaderTests, BcsTypesTests, BcsEncodeDecodeTests |
| **Cryptography** | SuiAddress.Normalize, Intent (normalize, MessageWithIntent), Blake2b.Hash256 | SuiAddressTests, IntentTests, Blake2bTests |
| **Sui BCS** | SuiBcsTypes (Address/ObjectId), ArgumentBcs (GasCoin, Input, Result, NestedResult), TypeTagSerializer (ParseFromStr, TagToString, NormalizeTypeTag) | SuiBcsTypesTests, ArgumentBcsTests, TypeTagSerializerTests |
| **Transactions** | TransactionHasher (GetDigestToSign, HashTypedData), TransactionDataBuilder (Build, BuildAndSerialize, SerializeToBcs roundtrip), Transaction (GetSerialized, Sign) | TransactionHasherTests, TransactionDataBuilderTests, TransactionTests |
| **JsonRpc** | SuiRpcClient (GetCoins, QueryTransactionBlocks, WaitForTransaction — via mock), PaginatedCoinsResponse, PaginatedTransactionBlocksResponse | SuiRpcClientTests, PaginatedCoinsResponseTests, PaginatedTransactionBlocksResponseTests |
| **Client** | SuiClient.SignAndExecuteTransactionBlockAsync(Transaction, Signer) with mock | SuiClientTests |

## Not covered (or only indirect)

| Area | Logic | Suggestion |
|------|--------|------------|
| **Cryptography** | Bech32 Encode/Decode | Bech32 is internal; not tested unless InternalsVisibleTo is added |
| **Cryptography** | Signature ToSerializedSignature / ParseSerializedKeypairSignature | **SignatureTests**: roundtrip, null checks |
| **Cryptography** | SuiPrivateKeyEncoding Encode/Decode | SuiPrivateKeyEncodingTests: roundtrip (optional) |
| **Sui BCS** | SuiBcsObjectRefs: ObjectDigest, SuiObjectRef, SharedObjectRef BCS | **ObjectRefsBcsTests**: serialize/parse roundtrip |
| **Sui BCS** | SuiBcsCallArg: CallArg Pure BCS, PureToBase64/PureFromBase64 | **CallArgBcsTests**: roundtrip for Pure, Base64 helper |
| **Sui BCS** | SuiBcsFundsWithdrawal, TypeTagBcs, StructTagBcs | Optional: BCS roundtrip tests |
| **Sui BCS** | ProgrammableMoveCallBcs, CommandBcs | Covered indirectly via full TransactionData roundtrip |
| **JsonRpc** | HttpTransport (real HTTP, error handling) | Integration or mock response tests |
| **JsonRpc** | PaginatedObjectsResponse, CoinBalance, SuiObjectResponse deserialization | Optional: JSON deserialize tests |
| **JsonRpc** | SuiRpcException, SuiHttpStatusException | Optional: throw/catch tests |
| **Keypairs** | Ed25519Keypair (Generate, Sign), Ed25519PublicKey (Verify) | Used in TransactionTests; optional direct Sign/Verify tests |

## Conclusion

Core flows and most public API are covered (Utils, BCS primitives, Sui address/intent, Signature roundtrip, Sui object refs and CallArg Pure BCS, transaction build/sign, RPC client with mock). Remaining gaps: Bech32 (internal), SuiPrivateKeyEncoding, CallArg Object/FundsWithdrawal BCS, HttpTransport/exception types, optional JSON deserialization for more DTOs. Coverage is high for user-facing logic.
