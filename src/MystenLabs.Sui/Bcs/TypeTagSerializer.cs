namespace MystenLabs.Sui.Bcs;

using System.Linq;
using System.Text.RegularExpressions;
using MystenLabs.Sui.Cryptography;

/// <summary>
/// Parses and serializes Move type tags to/from string form (e.g. "0x2::sui::SUI", "vector&lt;u8&gt;").
/// </summary>
public static class TypeTagSerializer
{
    private static readonly Regex VectorRegex = new(@"^vector<(.+)>$", RegexOptions.Compiled);
    private static readonly Regex StructRegex = new(@"^([^:]+)::([^:]+)::([^<]+)(<(.+)>)?$", RegexOptions.Compiled);

    /// <summary>
    /// Parses a type tag string into a <see cref="TypeTagValue"/>.
    /// </summary>
    /// <param name="value">Type string (e.g. "bool", "vector&lt;u8&gt;", "0x2::coin::Coin&lt;0x2::sui::SUI&gt;").</param>
    /// <param name="normalizeAddress">If true, normalize struct addresses with <see cref="SuiAddress.Normalize"/>.</param>
    /// <returns>Parsed type tag.</returns>
    /// <exception cref="ArgumentException">Thrown when the string cannot be parsed.</exception>
    public static TypeTagValue ParseFromStr(string value, bool normalizeAddress = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Type tag string cannot be null or empty.", nameof(value));
        }

        string trimmed = value.Trim();
        switch (trimmed)
        {
            case "address":
                return new TypeTagAddress();
            case "bool":
                return new TypeTagBool();
            case "u8":
                return new TypeTagU8();
            case "u16":
                return new TypeTagU16();
            case "u32":
                return new TypeTagU32();
            case "u64":
                return new TypeTagU64();
            case "u128":
                return new TypeTagU128();
            case "u256":
                return new TypeTagU256();
            case "signer":
                return new TypeTagSigner();
        }

        Match vectorMatch = VectorRegex.Match(trimmed);
        if (vectorMatch.Success)
        {
            TypeTagValue inner = ParseFromStr(vectorMatch.Groups[1].Value, normalizeAddress);
            return new TypeTagVector(inner);
        }

        Match structMatch = StructRegex.Match(trimmed);
        if (structMatch.Success)
        {
            string address = normalizeAddress
                ? SuiAddress.Normalize(structMatch.Groups[1].Value.AsSpan())
                : structMatch.Groups[1].Value;
            string module = structMatch.Groups[2].Value;
            string name = structMatch.Groups[3].Value;
            TypeTagValue[] typeParams = structMatch.Groups[5].Success
                ? ParseStructTypeArgs(structMatch.Groups[5].Value, normalizeAddress)
                : [];
            return new TypeTagStruct(new StructTag(address, module, name, typeParams));
        }

        throw new ArgumentException($"Encountered unexpected token when parsing type args for {trimmed}.", nameof(value));
    }

    /// <summary>
    /// Splits a comma-separated list of type arguments, respecting nested angle brackets.
    /// </summary>
    /// <param name="value">e.g. "0x2::sui::SUI, vector&lt;u8&gt;".</param>
    /// <param name="normalizeAddress">Whether to normalize addresses in each parsed type.</param>
    /// <returns>Array of parsed type tags.</returns>
    public static TypeTagValue[] ParseStructTypeArgs(string value, bool normalizeAddress = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        string[] tokens = SplitGenericParameters(value);
        var result = new TypeTagValue[tokens.Length];
        for (int index = 0; index < tokens.Length; index++)
        {
            result[index] = ParseFromStr(tokens[index], normalizeAddress);
        }

        return result;
    }

    /// <summary>
    /// Converts a type tag to its string representation.
    /// </summary>
    /// <param name="tag">The type tag.</param>
    /// <returns>String form (e.g. "bool", "vector&lt;u8&gt;", "0x2::coin::Coin&lt;0x2::sui::SUI&gt;").</returns>
    public static string TagToString(TypeTagValue tag)
    {
        if (tag == null)
        {
            throw new ArgumentNullException(nameof(tag));
        }

        return tag switch
        {
            TypeTagBool => "bool",
            TypeTagU8 => "u8",
            TypeTagU16 => "u16",
            TypeTagU32 => "u32",
            TypeTagU64 => "u64",
            TypeTagU128 => "u128",
            TypeTagU256 => "u256",
            TypeTagAddress => "address",
            TypeTagSigner => "signer",
            TypeTagVector vector => "vector<" + TagToString(vector.Inner) + ">",
            TypeTagStruct structTag => StructTagToString(structTag.Struct),
            _ => throw new ArgumentException("Invalid TypeTag.", nameof(tag))
        };
    }

    /// <summary>
    /// Normalizes a type tag string by parsing and serializing it back (addresses normalized).
    /// </summary>
    public static string NormalizeTypeTag(string value)
    {
        TypeTagValue tag = ParseFromStr(value, normalizeAddress: true);
        return TagToString(tag);
    }

    /// <summary>
    /// Parses a struct tag string (e.g. "0x2::coin::Coin&lt;0x2::sui::SUI&gt;") into a <see cref="StructTag"/>.
    /// </summary>
    /// <param name="value">Struct tag string.</param>
    /// <param name="normalizeAddress">If true, normalize the address with <see cref="SuiAddress.Normalize"/>.</param>
    /// <returns>Parsed struct tag.</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a struct type.</exception>
    public static StructTag ParseStructTag(string value, bool normalizeAddress = false)
    {
        TypeTagValue tag = ParseFromStr(value, normalizeAddress);
        if (tag is TypeTagStruct structTag)
        {
            return structTag.Struct;
        }

        throw new ArgumentException($"Expected a struct type, got: {value}", nameof(value));
    }

    /// <summary>
    /// Normalizes a struct tag string (parse and serialize with normalized addresses). Same as <see cref="NormalizeTypeTag"/> for struct types.
    /// </summary>
    public static string NormalizeStructTag(string value)
    {
        return NormalizeTypeTag(value);
    }

    private static string StructTagToString(StructTag structTag)
    {
        string typeParams = structTag.TypeParams.Length > 0
            ? "<" + string.Join(", ", structTag.TypeParams.Select(TagToString)) + ">"
            : string.Empty;
        return $"{structTag.Address}::{structTag.Module}::{structTag.Name}{typeParams}";
    }

    /// <summary>
    /// Splits a string by top-level commas, respecting nested angle brackets.
    /// </summary>
    private static string[] SplitGenericParameters(string value)
    {
        int nested = 0;
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (char character in value)
        {
            if (character == '<')
            {
                nested++;
                current.Append(character);
            }
            else if (character == '>')
            {
                nested--;
                current.Append(character);
            }
            else if (nested == 0 && character == ',')
            {
                tokens.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        tokens.Add(current.ToString().Trim());
        return tokens.ToArray();
    }
}
