namespace MystenLabs.Sui.Tests.Cryptography;

using System.Linq;
using MystenLabs.Sui.Cryptography;
using MystenLabs.Sui.Keypairs.Ed25519;
using MystenLabs.Sui.Keypairs.Secp256k1;
using MystenLabs.Sui.Keypairs.Secp256r1;
using Xunit;

public sealed class MnemonicsTests
{
    [Fact]
    public void IsValidHardenedPath_ValidPath_ReturnsTrue()
    {
        Assert.True(Mnemonics.IsValidHardenedPath("m/44'/784'/0'/0'/0'"));
        Assert.True(Mnemonics.IsValidHardenedPath("m/44'/784'/1'/0'/2'"));
    }

    [Fact]
    public void IsValidHardenedPath_InvalidPath_ReturnsFalse()
    {
        Assert.False(Mnemonics.IsValidHardenedPath("m/54'/784'/0'/0'/0'"));
        Assert.False(Mnemonics.IsValidHardenedPath("m/44'/784'/0'/0'/0"));
        Assert.False(Mnemonics.IsValidHardenedPath("m/44'/784'/0'/0/0'"));
        Assert.False(Mnemonics.IsValidHardenedPath(""));
        Assert.False(Mnemonics.IsValidHardenedPath("m/44'/784'/0'/0'"));
    }

    [Fact]
    public void IsValidBIP32Path_ValidPaths_ReturnsTrue()
    {
        Assert.True(Mnemonics.IsValidBIP32Path("m/54'/784'/0'/0/0"));
        Assert.True(Mnemonics.IsValidBIP32Path("m/74'/784'/0'/0/0"));
    }

    [Fact]
    public void IsValidBIP32Path_InvalidPath_ReturnsFalse()
    {
        Assert.False(Mnemonics.IsValidBIP32Path("m/44'/784'/0'/0'/0'"));
        Assert.False(Mnemonics.IsValidBIP32Path("m/54'/784'/0'/0'/0'"));
    }

    [Fact]
    public void MnemonicToSeed_Produces64Bytes()
    {
        string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        byte[] seed = Mnemonics.MnemonicToSeed(mnemonic);
        Assert.Equal(64, seed.Length);
    }

    [Fact]
    public void MnemonicToSeedHex_ReturnsHexString()
    {
        string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        string hex = Mnemonics.MnemonicToSeedHex(mnemonic);
        Assert.Equal(128, hex.Length);
        Assert.True(hex.All(static character => char.IsAsciiHexDigit(character)));
    }

    [Fact]
    public void DeriveKeypair_SameMnemonicAndPath_ProducesSameKeypair()
    {
        string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        Ed25519Keypair first = Ed25519Keypair.DeriveKeypair(mnemonic);
        Ed25519Keypair second = Ed25519Keypair.DeriveKeypair(mnemonic);
        Assert.Equal(first.GetPublicKey().ToRawBytes(), second.GetPublicKey().ToRawBytes());
    }

    [Fact]
    public void DeriveKeypairFromSeed_SeedBytes_SameAsMnemonicDerivation()
    {
        string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        byte[] seed = Mnemonics.MnemonicToSeed(mnemonic);
        Ed25519Keypair fromMnemonic = Ed25519Keypair.DeriveKeypair(mnemonic);
        Ed25519Keypair fromSeed = Ed25519Keypair.DeriveKeypairFromSeed(seed);
        Assert.Equal(fromMnemonic.GetPublicKey().ToRawBytes(), fromSeed.GetPublicKey().ToRawBytes());
    }

    [Fact]
    public void DeriveKeypair_InvalidPath_Throws()
    {
        string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        Assert.Throws<ArgumentException>(() => Ed25519Keypair.DeriveKeypair(mnemonic, "m/54'/784'/0'/0/0"));
    }

    [Fact]
    public void Secp256k1_DeriveKeypair_SameMnemonicAndPath_ProducesSameKeypair()
    {
        string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        Secp256k1Keypair first = Secp256k1Keypair.DeriveKeypair(mnemonic);
        Secp256k1Keypair second = Secp256k1Keypair.DeriveKeypair(mnemonic);
        Assert.Equal(first.GetPublicKey().ToRawBytes(), second.GetPublicKey().ToRawBytes());
    }

    [Fact]
    public void Secp256r1_DeriveKeypair_SameMnemonicAndPath_ProducesSameKeypair()
    {
        string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        Secp256r1Keypair first = Secp256r1Keypair.DeriveKeypair(mnemonic);
        Secp256r1Keypair second = Secp256r1Keypair.DeriveKeypair(mnemonic);
        Assert.Equal(first.GetPublicKey().ToRawBytes(), second.GetPublicKey().ToRawBytes());
    }

    [Fact]
    public void Secp256k1_DeriveKeypair_InvalidPath_Throws()
    {
        string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        Assert.Throws<ArgumentException>(() => Secp256k1Keypair.DeriveKeypair(mnemonic, "m/44'/784'/0'/0'/0'"));
    }
}
