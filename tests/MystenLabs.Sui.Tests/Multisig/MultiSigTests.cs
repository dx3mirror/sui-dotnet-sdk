namespace MystenLabs.Sui.Tests.Multisig;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Keypairs.Ed25519;
using MystenLabs.Sui.Multisig;
using MystenLabs.Sui.Verify;
using Xunit;

public sealed class MultiSigTests
{
    [Fact]
    public void FromPublicKeys_TwoKeys_Threshold2_Succeeds()
    {
        Ed25519Keypair key1 = Ed25519Keypair.Generate();
        Ed25519Keypair key2 = Ed25519Keypair.Generate();
        PublicKey pub1 = key1.GetPublicKey();
        PublicKey pub2 = key2.GetPublicKey();

        MultiSigPublicKey multisigPk = MultiSigPublicKey.FromPublicKeys(
            2,
            [(pub1, 1), (pub2, 1)]);

        Assert.Equal(2, multisigPk.GetThreshold());
        Assert.Equal(2, multisigPk.GetPublicKeys().Count);
        Assert.Equal(SignatureScheme.MultiSig, (SignatureScheme)multisigPk.Flag());
    }

    [Fact]
    public async Task CombinePartialSignatures_And_Verify_RoundTrip()
    {
        Ed25519Keypair key1 = Ed25519Keypair.Generate();
        Ed25519Keypair key2 = Ed25519Keypair.Generate();
        MultiSigPublicKey multisigPk = MultiSigPublicKey.FromPublicKeys(
            2,
            [(key1.GetPublicKey(), 1), (key2.GetPublicKey(), 1)]);

        byte[] digest = new byte[32];
        digest[0] = 1;

        string sig1 = Signature.ToSerializedSignature(
            key1.GetKeyScheme(),
            key1.Sign(digest),
            key1.GetPublicKey());
        string sig2 = Signature.ToSerializedSignature(
            key2.GetKeyScheme(),
            key2.Sign(digest),
            key2.GetPublicKey());

        string combined = multisigPk.CombinePartialSignatures([sig1, sig2]);
        Assert.False(string.IsNullOrEmpty(combined));

        bool valid = await multisigPk.VerifyAsync(digest, combined);
        Assert.True(valid);
    }

    [Fact]
    public async Task VerifySignatureAsync_MultiSig_ReturnsMultiSigPublicKey()
    {
        Ed25519Keypair key1 = Ed25519Keypair.Generate();
        Ed25519Keypair key2 = Ed25519Keypair.Generate();
        MultiSigPublicKey multisigPk = MultiSigPublicKey.FromPublicKeys(
            2,
            [(key1.GetPublicKey(), 1), (key2.GetPublicKey(), 1)]);

        byte[] digest = new byte[32];
        digest[0] = 2;
        string sig1 = Signature.ToSerializedSignature(key1.GetKeyScheme(), key1.Sign(digest), key1.GetPublicKey());
        string sig2 = Signature.ToSerializedSignature(key2.GetKeyScheme(), key2.Sign(digest), key2.GetPublicKey());
        string combined = multisigPk.CombinePartialSignatures([sig1, sig2]);

        PublicKey recovered = await SuiVerify.VerifySignatureAsync(digest, combined);
        Assert.IsType<MultiSigPublicKey>(recovered);
        Assert.True(recovered.ToSuiAddress() == multisigPk.ToSuiAddress());
    }

    [Fact]
    public async Task MultiSigSigner_SignTransaction_ProducesValidMultisig()
    {
        Ed25519Keypair key1 = Ed25519Keypair.Generate();
        Ed25519Keypair key2 = Ed25519Keypair.Generate();
        MultiSigPublicKey multisigPk = MultiSigPublicKey.FromPublicKeys(
            2,
            [(key1.GetPublicKey(), 1), (key2.GetPublicKey(), 1)]);

        var signer = new MultiSigSigner(multisigPk, [key1, key2]);
        byte[] txBytes = [0x00, 0x01, 0x02];
        SignatureWithBytes result = signer.SignTransaction(txBytes);

        Assert.NotNull(result.Signature);
        Assert.Equal(SignatureScheme.MultiSig, signer.GetKeyScheme());

        byte[] intentMessage = Intent.MessageWithIntent(IntentScope.TransactionData, txBytes);
        byte[] digest = Blake2b.Hash256(intentMessage);
        bool valid = await multisigPk.VerifyAsync(digest, result.Signature);
        Assert.True(valid);
    }

    [Fact]
    public void FromPublicKeys_ThresholdExceedsTotalWeight_Throws()
    {
        Ed25519Keypair key1 = Ed25519Keypair.Generate();
        Assert.Throws<ArgumentException>(() =>
            MultiSigPublicKey.FromPublicKeys(3, [(key1.GetPublicKey(), 1)]));
    }
}
