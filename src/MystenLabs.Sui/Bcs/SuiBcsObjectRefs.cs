namespace MystenLabs.Sui.Bcs;

using MystenLabs.Sui.Utils;

/// <summary>
/// Object digest length in bytes (32 bytes, serialized as length-prefixed vector; string representation is Base58).
/// </summary>
public static class ObjectDigestConstants
{
    /// <summary>
    /// Required length of an object digest in bytes.
    /// </summary>
    public const int DigestLength = 32;
}

/// <summary>
/// BCS type for Sui object digest (32 bytes). Input/output as Base58 string (e.g. from RPC).
/// </summary>
public static class ObjectDigestBcs
{
    /// <summary>
    /// BCS type for ObjectDigest: length-prefixed byte vector of 32 bytes; string value is Base58.
    /// </summary>
    public static BcsType<string> ObjectDigest { get; } = new BcsType<string>(
        "ObjectDigest",
        reader =>
        {
            int length = (int)reader.ReadUleb128();
            if (length != ObjectDigestConstants.DigestLength)
            {
                throw new InvalidOperationException(
                    $"ObjectDigest must be {ObjectDigestConstants.DigestLength} bytes, got {length}.");
            }

            byte[] bytes = reader.ReadBytes(length);
            return Base58.Encode(bytes);
        },
        (value, writer) =>
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("ObjectDigest cannot be null or empty.", nameof(value));
            }

            byte[] bytes = Base58.Decode(value.AsSpan());
            if (bytes.Length != ObjectDigestConstants.DigestLength)
            {
                throw new ArgumentException(
                    $"ObjectDigest must be {ObjectDigestConstants.DigestLength} bytes after decode, got {bytes.Length}.",
                    nameof(value));
            }

            writer.WriteUleb128((ulong)bytes.Length);
            writer.WriteBytes(bytes);
        },
        _ => null,
        value =>
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("ObjectDigest cannot be null or empty.", nameof(value));
            }

            byte[] bytes = Base58.Decode(value.AsSpan());
            if (bytes.Length != ObjectDigestConstants.DigestLength)
            {
                throw new ArgumentException(
                    $"ObjectDigest must be {ObjectDigestConstants.DigestLength} bytes.",
                    nameof(value));
            }
        });
}

/// <summary>
/// Sui object reference: object ID, version, and digest (used for owned/immutable and receiving objects).
/// </summary>
/// <param name="ObjectId">Normalized address (0x + 64 hex) of the object.</param>
/// <param name="Version">Object version number.</param>
/// <param name="Digest">Object digest as Base58 string.</param>
public sealed record SuiObjectRef(string ObjectId, ulong Version, string Digest);

/// <summary>
/// Shared object reference: object ID, initial shared version, and mutability.
/// </summary>
/// <param name="ObjectId">Normalized address of the object.</param>
/// <param name="InitialSharedVersion">Version at which the object was shared.</param>
/// <param name="Mutable">Whether the shared reference allows mutation.</param>
public sealed record SharedObjectRef(string ObjectId, ulong InitialSharedVersion, bool Mutable);

/// <summary>
/// BCS serialization for <see cref="SuiObjectRef"/> and <see cref="SharedObjectRef"/>.
/// </summary>
public static class SuiObjectRefBcs
{
    /// <summary>
    /// BCS type for SuiObjectRef (struct: Address, u64, ObjectDigest).
    /// </summary>
    public static BcsType<SuiObjectRef> SuiObjectRef { get; } = new BcsType<SuiObjectRef>(
        "SuiObjectRef",
        reader =>
        {
            string objectId = SuiBcsTypes.Address.Read(reader);
            ulong version = reader.Read64();
            string digest = ObjectDigestBcs.ObjectDigest.Read(reader);
            return new SuiObjectRef(objectId, version, digest);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            SuiBcsTypes.Address.Write(value.ObjectId, writer);
            writer.WriteU64(value.Version);
            ObjectDigestBcs.ObjectDigest.Write(value.Digest, writer);
        },
        value =>
        {
            if (value == null)
            {
                return null;
            }

            int? addressSize = SuiBcsTypes.Address.GetSerializedSize(value.ObjectId);
            int? digestSize = ObjectDigestBcs.ObjectDigest.GetSerializedSize(value.Digest);
            if (addressSize == null || digestSize == null)
            {
                return null;
            }

            return addressSize.Value + 8 + digestSize.Value;
        },
        value =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        });

    /// <summary>
    /// BCS type for SharedObjectRef (struct: Address, u64, bool).
    /// </summary>
    public static BcsType<SharedObjectRef> SharedObjectRef { get; } = new BcsType<SharedObjectRef>(
        "SharedObjectRef",
        reader =>
        {
            string objectId = SuiBcsTypes.Address.Read(reader);
            ulong initialSharedVersion = reader.Read64();
            bool mutable = reader.Read8() != 0;
            return new SharedObjectRef(objectId, initialSharedVersion, mutable);
        },
        (value, writer) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            SuiBcsTypes.Address.Write(value.ObjectId, writer);
            writer.WriteU64(value.InitialSharedVersion);
            writer.WriteU8((byte)(value.Mutable ? 1 : 0));
        },
        value =>
        {
            if (value == null)
            {
                return null;
            }

            int? addressSize = SuiBcsTypes.Address.GetSerializedSize(value.ObjectId);
            if (addressSize == null)
            {
                return null;
            }

            return addressSize.Value + 8 + 1;
        },
        value =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        });
}

/// <summary>
/// Object argument for Move calls: either an owned/immutable object, a shared object, or a receiving object.
/// </summary>
public abstract record ObjectArg;

/// <summary>
/// Owned or immutable object argument.
/// </summary>
public sealed record ObjectArgImmOrOwned(SuiObjectRef Value) : ObjectArg;

/// <summary>
/// Shared object argument.
/// </summary>
public sealed record ObjectArgShared(SharedObjectRef Value) : ObjectArg;

/// <summary>
/// Receiving object argument (e.g. for receiving a coin).
/// </summary>
public sealed record ObjectArgReceiving(SuiObjectRef Value) : ObjectArg;

/// <summary>
/// BCS serialization for <see cref="ObjectArg"/> (enum: ImmOrOwnedObject, SharedObject, Receiving).
/// </summary>
public static class ObjectArgBcs
{
    private const int VariantImmOrOwnedObject = 0;
    private const int VariantSharedObject = 1;
    private const int VariantReceiving = 2;

    /// <summary>
    /// BCS type for ObjectArg.
    /// </summary>
    public static BcsType<ObjectArg> ObjectArg { get; } = new BcsType<ObjectArg>(
        "ObjectArg",
        reader =>
        {
            ulong index = reader.ReadUleb128();
            return index switch
            {
                VariantImmOrOwnedObject => new ObjectArgImmOrOwned(SuiObjectRefBcs.SuiObjectRef.Read(reader)),
                VariantSharedObject => new ObjectArgShared(SuiObjectRefBcs.SharedObjectRef.Read(reader)),
                VariantReceiving => new ObjectArgReceiving(SuiObjectRefBcs.SuiObjectRef.Read(reader)),
                _ => throw new InvalidOperationException($"Unknown ObjectArg variant index: {index}.")
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
                case ObjectArgImmOrOwned immOrOwned:
                    writer.WriteUleb128(VariantImmOrOwnedObject);
                    SuiObjectRefBcs.SuiObjectRef.Write(immOrOwned.Value, writer);
                    break;
                case ObjectArgShared shared:
                    writer.WriteUleb128(VariantSharedObject);
                    SuiObjectRefBcs.SharedObjectRef.Write(shared.Value, writer);
                    break;
                case ObjectArgReceiving receiving:
                    writer.WriteUleb128(VariantReceiving);
                    SuiObjectRefBcs.SuiObjectRef.Write(receiving.Value, writer);
                    break;
                default:
                    throw new ArgumentException($"Unknown ObjectArg type: {value.GetType().Name}.", nameof(value));
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
