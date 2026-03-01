namespace MystenLabs.Sui.Multisig;

using MystenLabs.Sui.Cryptography;

/// <summary>
/// One entry in the multisig public key map: scheme, raw public key bytes, and weight.
/// </summary>
/// <param name="Scheme">Signature scheme (e.g. Ed25519).</param>
/// <param name="PublicKeyBytes">Raw public key bytes (length depends on scheme).</param>
/// <param name="Weight">Weight of this key in the multisig threshold.</param>
public sealed record MultiSigPkMapEntry(SignatureScheme Scheme, byte[] PublicKeyBytes, byte Weight);

/// <summary>
/// Multisig public key BCS structure: list of (pubkey, weight) and threshold.
/// </summary>
/// <param name="PkMap">Public keys and their weights.</param>
/// <param name="Threshold">Minimum combined weight required to form a valid signature.</param>
public sealed record MultiSigPublicKeyStruct(IReadOnlyList<MultiSigPkMapEntry> PkMap, ushort Threshold);

/// <summary>
/// One compressed signature in a multisig (scheme + raw signature bytes).
/// </summary>
/// <param name="Scheme">Signature scheme.</param>
/// <param name="SignatureBytes">Raw signature bytes.</param>
public sealed record CompressedSignatureEntry(SignatureScheme Scheme, byte[] SignatureBytes);

/// <summary>
/// Full multisig BCS structure: partial signatures, bitmap of signer indices, and the multisig public key.
/// </summary>
/// <param name="Sigs">Compressed signatures (order matches bitmap indices).</param>
/// <param name="Bitmap">Bits indicate which signers contributed (bit at index = 1 means publicKeyMap[index] signed).</param>
/// <param name="MultisigPk">The multisig public key this signature is for.</param>
public sealed record MultiSigStruct(
    IReadOnlyList<CompressedSignatureEntry> Sigs,
    ushort Bitmap,
    MultiSigPublicKeyStruct MultisigPk);
