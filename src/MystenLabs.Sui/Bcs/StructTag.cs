namespace MystenLabs.Sui.Bcs;

/// <summary>
/// Move struct identifier: address::module::name with optional type parameters.
/// </summary>
/// <param name="Address">Package address (normalized 0x + 64 hex).</param>
/// <param name="Module">Module name.</param>
/// <param name="Name">Struct name.</param>
/// <param name="TypeParams">Type arguments (e.g. for GenericStruct&lt;T&gt;).</param>
public sealed record StructTag(
    string Address,
    string Module,
    string Name,
    TypeTagValue[] TypeParams);
