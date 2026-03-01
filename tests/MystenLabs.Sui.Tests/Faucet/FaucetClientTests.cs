namespace MystenLabs.Sui.Tests.Faucet;

using MystenLabs.Sui.Faucet;
using Xunit;

public sealed class FaucetClientTests
{
    [Theory]
    [InlineData("testnet", FaucetClient.HostTestnet)]
    [InlineData("devnet", FaucetClient.HostDevnet)]
    [InlineData("localnet", FaucetClient.HostLocalnet)]
    [InlineData("TESTNET", FaucetClient.HostTestnet)]
    [InlineData("DEVNET", FaucetClient.HostDevnet)]
    public void GetFaucetHost_ReturnsExpectedUrl(string network, string expectedHost)
    {
        string host = FaucetClient.GetFaucetHost(network);
        Assert.Equal(expectedHost, host);
    }

    [Fact]
    public void GetFaucetHost_UnknownNetwork_Throws()
    {
        Assert.Throws<ArgumentException>(() => FaucetClient.GetFaucetHost("mainnet"));
        Assert.Throws<ArgumentException>(() => FaucetClient.GetFaucetHost("unknown"));
    }

    [Fact]
    public void FaucetRateLimitException_DefaultMessage_IsSet()
    {
        var exception = new FaucetRateLimitException();
        Assert.Contains("retry", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
