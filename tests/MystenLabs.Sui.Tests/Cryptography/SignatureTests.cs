namespace MystenLabs.Sui.Tests.Cryptography;

using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Keypairs.Ed25519;
using MystenLabs.Sui.Keypairs.Secp256k1;
using MystenLabs.Sui.Keypairs.Secp256r1;
using Xunit;

public sealed class SignatureTests
{
    [Fact]
    public void ToSerializedSignature_ParseSerializedKeypairSignature_Ed25519_RoundTrip()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        byte[] digest = new byte[32];
        digest[0] = 1;
        byte[] signature = keypair.Sign(digest);
        string serialized = Signature.ToSerializedSignature(
            keypair.GetKeyScheme(),
            signature,
            keypair.GetPublicKey());
        Assert.False(string.IsNullOrEmpty(serialized));

        (SignatureScheme scheme, byte[] parsedSignature, byte[] parsedPublicKey) =
            Signature.ParseSerializedKeypairSignature(serialized);
        Assert.Equal(keypair.GetKeyScheme(), scheme);
        Assert.Equal(signature, parsedSignature);
        Assert.Equal(keypair.GetPublicKey().ToRawBytes(), parsedPublicKey);
    }

    [Fact]
    public void ToSerializedSignature_ParseSerializedKeypairSignature_Secp256k1_RoundTrip()
    {
        Secp256k1Keypair keypair = Secp256k1Keypair.Generate();
        byte[] digest = new byte[32];
        digest[0] = 1;
        byte[] signature = keypair.Sign(digest);
        string serialized = Signature.ToSerializedSignature(
            keypair.GetKeyScheme(),
            signature,
            keypair.GetPublicKey());
        Assert.False(string.IsNullOrEmpty(serialized));

        (SignatureScheme scheme, byte[] parsedSignature, byte[] parsedPublicKey) =
            Signature.ParseSerializedKeypairSignature(serialized);
        Assert.Equal(keypair.GetKeyScheme(), scheme);
        Assert.Equal(signature, parsedSignature);
        Assert.Equal(keypair.GetPublicKey().ToRawBytes(), parsedPublicKey);
    }

    [Fact]
    public void ToSerializedSignature_ParseSerializedKeypairSignature_Secp256r1_RoundTrip()
    {
        Secp256r1Keypair keypair = Secp256r1Keypair.Generate();
        byte[] digest = new byte[32];
        digest[0] = 1;
        byte[] signature = keypair.Sign(digest);
        string serialized = Signature.ToSerializedSignature(
            keypair.GetKeyScheme(),
            signature,
            keypair.GetPublicKey());
        Assert.False(string.IsNullOrEmpty(serialized));

        (SignatureScheme scheme, byte[] parsedSignature, byte[] parsedPublicKey) =
            Signature.ParseSerializedKeypairSignature(serialized);
        Assert.Equal(keypair.GetKeyScheme(), scheme);
        Assert.Equal(signature, parsedSignature);
        Assert.Equal(keypair.GetPublicKey().ToRawBytes(), parsedPublicKey);
    }

    [Fact]
    public void ParseSerializedKeypairSignature_Empty_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Signature.ParseSerializedKeypairSignature(string.Empty));
    }

    [Fact]
    public void ToSerializedSignature_Null_PublicKey_Throws()
    {
        Ed25519Keypair keypair = Ed25519Keypair.Generate();
        byte[] signature = keypair.Sign(new byte[32]);
        Assert.Throws<ArgumentNullException>(() =>
            Signature.ToSerializedSignature(keypair.GetKeyScheme(), signature, null!));
    }
}
