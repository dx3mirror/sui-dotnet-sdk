namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Move type tag (bool, u8, u16, u32, u64, u128, u256, address, signer, vector, struct).
/// </summary>
public abstract record TypeTagValue;

/// <summary>
/// bool
/// </summary>
public sealed record TypeTagBool : TypeTagValue;

/// <summary>
/// u8
/// </summary>
public sealed record TypeTagU8 : TypeTagValue;

/// <summary>
/// u16
/// </summary>
public sealed record TypeTagU16 : TypeTagValue;

/// <summary>
/// u32
/// </summary>
public sealed record TypeTagU32 : TypeTagValue;

/// <summary>
/// u64
/// </summary>
public sealed record TypeTagU64 : TypeTagValue;

/// <summary>
/// u128
/// </summary>
public sealed record TypeTagU128 : TypeTagValue;

/// <summary>
/// u256
/// </summary>
public sealed record TypeTagU256 : TypeTagValue;

/// <summary>
/// address
/// </summary>
public sealed record TypeTagAddress : TypeTagValue;

/// <summary>
/// signer
/// </summary>
public sealed record TypeTagSigner : TypeTagValue;

/// <summary>
/// vector of inner type
/// </summary>
/// <param name="Inner">Element type.</param>
public sealed record TypeTagVector(TypeTagValue Inner) : TypeTagValue;

/// <summary>
/// struct type
/// </summary>
/// <param name="Struct">Struct tag.</param>
public sealed record TypeTagStruct(StructTag Struct) : TypeTagValue;
