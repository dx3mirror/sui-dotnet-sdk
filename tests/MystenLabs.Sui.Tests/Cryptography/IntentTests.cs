namespace MystenLabs.Sui.Tests.Cryptography;

using MystenLabs.Sui.Cryptography;
using Xunit;

public sealed class IntentTests
{
    [Fact]
    public void MessageWithIntent_Produces_NonEmpty_Bytes()
    {
        byte[] message = { 0x01, 0x02, 0x03 };
        byte[] result = Intent.MessageWithIntent(IntentScope.TransactionData, message);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void MessageWithIntent_Same_Input_Same_Output()
    {
        byte[] message = { 0x61, 0x62 };
        byte[] result1 = Intent.MessageWithIntent(IntentScope.PersonalMessage, message);
        byte[] result2 = Intent.MessageWithIntent(IntentScope.PersonalMessage, message);
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void MessageWithIntent_Different_Scope_Different_Output()
    {
        byte[] message = { 0x01 };
        byte[] resultData = Intent.MessageWithIntent(IntentScope.TransactionData, message);
        byte[] resultPersonal = Intent.MessageWithIntent(IntentScope.PersonalMessage, message);
        Assert.NotEqual(resultData, resultPersonal);
    }
}
