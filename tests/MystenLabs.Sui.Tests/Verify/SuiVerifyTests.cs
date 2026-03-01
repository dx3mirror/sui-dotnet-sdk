namespace MystenLabs.Sui.Tests.Verify;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Keypairs.Ed25519;
using MystenLabs.Sui.Keypairs.Secp256k1;
using MystenLabs.Sui.Keypairs.Secp256r1;
using MystenLabs.Sui.Transactions;
using MystenLabs.Sui.Verify;
using Xunit;

public sealed class SuiVerifyTests
{
    [Fact]
    public void PublicKeyFromRawBytes_Ed25519_ReturnsEd25519PublicKey()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        byte[] raw = keypair.GetPublicKey().ToRawBytes();
        PublicKey recovered = SuiVerify.PublicKeyFromRawBytes(SignatureScheme.Ed25519, raw);
        Assert.True(recovered.Equals(keypair.GetPublicKey()));
        Assert.Equal(keypair.GetPublicKey().ToSuiAddress(), recovered.ToSuiAddress());
    }

    [Fact]
    public void PublicKeyFromRawBytes_Secp256k1_ReturnsSecp256k1PublicKey()
    {
        Secp256k1Keypair keypair = Secp256k1Keypair.Generate();
        byte[] raw = keypair.GetPublicKey().ToRawBytes();
        PublicKey recovered = SuiVerify.PublicKeyFromRawBytes(SignatureScheme.Secp256k1, raw);
        Assert.True(recovered.Equals(keypair.GetPublicKey()));
        Assert.Equal(keypair.GetPublicKey().ToSuiAddress(), recovered.ToSuiAddress());
    }

    [Fact]
    public void PublicKeyFromRawBytes_Secp256r1_ReturnsSecp256r1PublicKey()
    {
        Secp256r1Keypair keypair = Secp256r1Keypair.Generate();
        byte[] raw = keypair.GetPublicKey().ToRawBytes();
        PublicKey recovered = SuiVerify.PublicKeyFromRawBytes(SignatureScheme.Secp256r1, raw);
        Assert.True(recovered.Equals(keypair.GetPublicKey()));
        Assert.Equal(keypair.GetPublicKey().ToSuiAddress(), recovered.ToSuiAddress());
    }

    [Fact]
    public void PublicKeyFromRawBytes_UnsupportedScheme_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SuiVerify.PublicKeyFromRawBytes(SignatureScheme.MultiSig, new byte[32]));
    }

    [Fact]
    public void PublicKeyFromSuiBytes_Base64_RoundTrip()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        string suiBase64 = keypair.GetPublicKey().ToSuiPublicKey();
        PublicKey recovered = SuiVerify.PublicKeyFromSuiBytes(suiBase64);
        Assert.True(recovered.Equals(keypair.GetPublicKey()));
    }

    [Fact]
    public void PublicKeyFromSuiBytes_WithMatchingAddress_Succeeds()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        string address = keypair.GetPublicKey().ToSuiAddress();
        PublicKey recovered = SuiVerify.PublicKeyFromSuiBytes(keypair.GetPublicKey().ToSuiPublicKey(), address);
        Assert.Equal(address, recovered.ToSuiAddress());
    }

    [Fact]
    public void PublicKeyFromSuiBytes_WithMismatchedAddress_Throws()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        string wrongAddress = "0x0000000000000000000000000000000000000000000000000000000000000001";
        Assert.Throws<ArgumentException>(() =>
            SuiVerify.PublicKeyFromSuiBytes(keypair.GetPublicKey().ToSuiPublicKey(), wrongAddress));
    }

    [Fact]
    public async Task VerifySignatureAsync_ValidSignature_ReturnsPublicKey()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        byte[] digest = new byte[32];
        digest[0] = 1;
        byte[] signature = keypair.Sign(digest);
        string serialized = Signature.ToSerializedSignature(
            keypair.GetKeyScheme(),
            signature,
            keypair.GetPublicKey());

        PublicKey recovered = await SuiVerify.VerifySignatureAsync(digest, serialized);
        Assert.True(recovered.Equals(keypair.GetPublicKey()));
    }

    [Fact]
    public async Task VerifySignatureAsync_WithMatchingAddress_Succeeds()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        byte[] digest = new byte[32];
        byte[] signature = keypair.Sign(digest);
        string serialized = Signature.ToSerializedSignature(
            keypair.GetKeyScheme(),
            signature,
            keypair.GetPublicKey());
        string address = keypair.GetPublicKey().ToSuiAddress();

        PublicKey recovered = await SuiVerify.VerifySignatureAsync(digest, serialized, address);
        Assert.Equal(address, recovered.ToSuiAddress());
    }

    [Fact]
    public async Task VerifySignatureAsync_InvalidSignature_Throws()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        byte[] digest = new byte[32];
        byte[] signature = keypair.Sign(digest);
        string serialized = Signature.ToSerializedSignature(
            keypair.GetKeyScheme(),
            signature,
            keypair.GetPublicKey());

        byte[] wrongDigest = new byte[32];
        wrongDigest[0] = 2;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SuiVerify.VerifySignatureAsync(wrongDigest, serialized));
    }

    [Fact]
    public async Task VerifyTransactionSignatureAsync_ValidSignature_ReturnsPublicKey()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        byte[] transactionBytes = new byte[] { 0x00, 0x01, 0x02 };
        byte[] digest = TransactionHasher.GetDigestToSign(transactionBytes);
        byte[] signature = keypair.Sign(digest);
        string serialized = Signature.ToSerializedSignature(
            keypair.GetKeyScheme(),
            signature,
            keypair.GetPublicKey());

        PublicKey recovered = await SuiVerify.VerifyTransactionSignatureAsync(transactionBytes, serialized);
        Assert.True(recovered.Equals(keypair.GetPublicKey()));
    }

    [Fact]
    public async Task VerifyPersonalMessageSignatureAsync_ValidSignature_ReturnsPublicKey()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        byte[] message = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        byte[] intentMessage = Intent.MessageWithIntent(IntentScope.PersonalMessage, message);
        byte[] digest = Blake2b.Hash256(intentMessage);
        byte[] signature = keypair.Sign(digest);
        string serialized = Signature.ToSerializedSignature(
            keypair.GetKeyScheme(),
            signature,
            keypair.GetPublicKey());

        PublicKey recovered = await SuiVerify.VerifyPersonalMessageSignatureAsync(message, serialized);
        Assert.True(recovered.Equals(keypair.GetPublicKey()));
    }

    [Fact]
    public async Task VerifySignatureAsync_Secp256k1_ValidSignature_ReturnsPublicKey()
    {
        Secp256k1Keypair keypair = Secp256k1Keypair.Generate();
        byte[] digest = new byte[32];
        digest[0] = 1;
        byte[] signature = keypair.Sign(digest);
        string serialized = Signature.ToSerializedSignature(
            keypair.GetKeyScheme(),
            signature,
            keypair.GetPublicKey());

        PublicKey recovered = await SuiVerify.VerifySignatureAsync(digest, serialized);
        Assert.True(recovered.Equals(keypair.GetPublicKey()));
    }

    [Fact]
    public async Task VerifySignatureAsync_Secp256r1_ValidSignature_ReturnsPublicKey()
    {
        Secp256r1Keypair keypair = Secp256r1Keypair.Generate();
        byte[] digest = new byte[32];
        digest[0] = 1;
        byte[] signature = keypair.Sign(digest);
        string serialized = Signature.ToSerializedSignature(
            keypair.GetKeyScheme(),
            signature,
            keypair.GetPublicKey());

        PublicKey recovered = await SuiVerify.VerifySignatureAsync(digest, serialized);
        Assert.True(recovered.Equals(keypair.GetPublicKey()));
    }
}
