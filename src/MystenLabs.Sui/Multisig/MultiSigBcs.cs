namespace MystenLabs.Sui.Multisig;

using MystenLabs.Sui.Bcs;
using MystenLabs.Sui.Cryptography;

/// <summary>
/// BCS serialization for MultiSig and MultiSigPublicKey (Sui multisig format).
/// </summary>
public static class MultiSigBcs
{
    private const int CompressedSigEd25519Size = 64;
    private const int CompressedSigSecp256k1Size = 64;
    private const int CompressedSigSecp256r1Size = 64;
    private const int PublicKeyEd25519Size = 32;
    private const int PublicKeySecp256k1Size = 33;
    private const int PublicKeySecp256r1Size = 33;
    private const int PublicKeyPasskeySize = 33;

    /// <summary>
    /// Parses BCS bytes into a MultiSig public key structure (pk_map + threshold).
    /// </summary>
    public static MultiSigPublicKeyStruct ParseMultiSigPublicKey(ReadOnlySpan<byte> data)
    {
        var reader = new BcsReader(data.ToArray());
        return ReadMultiSigPublicKey(reader);
    }

    /// <summary>
    /// Serializes a MultiSig public key structure to BCS bytes.
    /// </summary>
    public static byte[] SerializeMultiSigPublicKey(MultiSigPublicKeyStruct value)
    {
        var writer = new BcsWriter();
        WriteMultiSigPublicKey(writer, value);
        return writer.ToBytes();
    }

    /// <summary>
    /// Parses BCS bytes (after the MultiSig flag byte) into a MultiSig struct.
    /// </summary>
    public static MultiSigStruct ParseMultiSig(ReadOnlySpan<byte> data)
    {
        var reader = new BcsReader(data.ToArray());
        return ReadMultiSig(reader);
    }

    /// <summary>
    /// Serializes a MultiSig struct to BCS bytes (to be prefixed with MultiSig flag 0x03).
    /// </summary>
    public static byte[] SerializeMultiSig(MultiSigStruct value)
    {
        var writer = new BcsWriter();
        WriteMultiSig(writer, value);
        return writer.ToBytes();
    }

    private static MultiSigPublicKeyStruct ReadMultiSigPublicKey(BcsReader reader)
    {
        int pkMapLength = (int)reader.ReadUleb128();
        var pkMap = new List<MultiSigPkMapEntry>();
        for (int index = 0; index < pkMapLength; index++)
        {
            pkMap.Add(ReadPkMapEntry(reader));
        }

        ushort threshold = (ushort)reader.Read16();
        return new MultiSigPublicKeyStruct(pkMap, threshold);
    }

    private static void WriteMultiSigPublicKey(BcsWriter writer, MultiSigPublicKeyStruct value)
    {
        writer.WriteUleb128((ulong)value.PkMap.Count);
        foreach (MultiSigPkMapEntry entry in value.PkMap)
        {
            WritePkMapEntry(writer, entry);
        }

        writer.WriteU16(value.Threshold);
    }

    private static MultiSigPkMapEntry ReadPkMapEntry(BcsReader reader)
    {
        uint variant = (uint)reader.ReadUleb128();
        byte[] keyBytes = variant switch
        {
            0 => reader.ReadBytes(PublicKeyEd25519Size),
            1 => reader.ReadBytes(PublicKeySecp256k1Size),
            2 => reader.ReadBytes(PublicKeySecp256r1Size),
            3 => reader.ReadBytes((int)reader.ReadUleb128()),
            4 => reader.ReadBytes(PublicKeyPasskeySize),
            _ => throw new InvalidOperationException($"Unknown PublicKey enum variant: {variant}.")
        };

        byte weight = reader.Read8();
        var scheme = variant switch
        {
            0 => SignatureScheme.Ed25519,
            1 => SignatureScheme.Secp256k1,
            2 => SignatureScheme.Secp256r1,
            3 => SignatureScheme.ZkLogin,
            4 => SignatureScheme.Passkey,
            _ => throw new InvalidOperationException($"Unknown PublicKey enum variant: {variant}.")
        };

        return new MultiSigPkMapEntry(scheme, keyBytes, weight);
    }

    private static void WritePkMapEntry(BcsWriter writer, MultiSigPkMapEntry entry)
    {
        int variantIndex = entry.Scheme switch
        {
            SignatureScheme.Ed25519 => 0,
            SignatureScheme.Secp256k1 => 1,
            SignatureScheme.Secp256r1 => 2,
            SignatureScheme.ZkLogin => 3,
            SignatureScheme.Passkey => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(entry), $"Unsupported scheme in multisig pk_map: {entry.Scheme}.")
        };

        writer.WriteUleb128((ulong)variantIndex);
        writer.WriteBytes(entry.PublicKeyBytes);
        writer.WriteU8(entry.Weight);
    }

    private static MultiSigStruct ReadMultiSig(BcsReader reader)
    {
        int sigsLength = (int)reader.ReadUleb128();
        var sigs = new List<CompressedSignatureEntry>();
        for (int index = 0; index < sigsLength; index++)
        {
            sigs.Add(ReadCompressedSignature(reader));
        }

        ushort bitmap = (ushort)reader.Read16();
        MultiSigPublicKeyStruct multisigPk = ReadMultiSigPublicKey(reader);
        return new MultiSigStruct(sigs, bitmap, multisigPk);
    }

    private static void WriteMultiSig(BcsWriter writer, MultiSigStruct value)
    {
        writer.WriteUleb128((ulong)value.Sigs.Count);
        foreach (CompressedSignatureEntry signatureEntry in value.Sigs)
        {
            WriteCompressedSignature(writer, signatureEntry);
        }

        writer.WriteU16(value.Bitmap);
        WriteMultiSigPublicKey(writer, value.MultisigPk);
    }

    private static CompressedSignatureEntry ReadCompressedSignature(BcsReader reader)
    {
        uint variant = (uint)reader.ReadUleb128();
        byte[] signatureBytes = variant switch
        {
            0 => reader.ReadBytes(CompressedSigEd25519Size),
            1 => reader.ReadBytes(CompressedSigSecp256k1Size),
            2 => reader.ReadBytes(CompressedSigSecp256r1Size),
            3 => reader.ReadBytes((int)reader.ReadUleb128()),
            4 => reader.ReadBytes((int)reader.ReadUleb128()),
            _ => throw new InvalidOperationException($"Unknown CompressedSignature variant: {variant}.")
        };

        var scheme = variant switch
        {
            0 => SignatureScheme.Ed25519,
            1 => SignatureScheme.Secp256k1,
            2 => SignatureScheme.Secp256r1,
            3 => SignatureScheme.ZkLogin,
            4 => SignatureScheme.Passkey,
            _ => throw new InvalidOperationException($"Unknown CompressedSignature variant: {variant}.")
        };

        return new CompressedSignatureEntry(scheme, signatureBytes);
    }

    private static void WriteCompressedSignature(BcsWriter writer, CompressedSignatureEntry entry)
    {
        int variantIndex = entry.Scheme switch
        {
            SignatureScheme.Ed25519 => 0,
            SignatureScheme.Secp256k1 => 1,
            SignatureScheme.Secp256r1 => 2,
            SignatureScheme.ZkLogin => 3,
            SignatureScheme.Passkey => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(entry), $"Unsupported scheme in compressed signature: {entry.Scheme}.")
        };

        writer.WriteUleb128((ulong)variantIndex);
        if (variantIndex <= 2)
        {
            if (entry.SignatureBytes.Length != CompressedSigEd25519Size)
            {
                throw new ArgumentException($"Ed25519/Secp256k1/Secp256r1 signature must be {CompressedSigEd25519Size} bytes.", nameof(entry));
            }

            writer.WriteBytes(entry.SignatureBytes);
        }
        else
        {
            writer.WriteUleb128((ulong)entry.SignatureBytes.Length);
            writer.WriteBytes(entry.SignatureBytes);
        }
    }
}
