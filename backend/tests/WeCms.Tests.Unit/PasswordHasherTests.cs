using WeCms.Infrastructure.Security;
using WeCms.Shared.Contracts;
using Xunit;

namespace WeCms.Tests.Unit;

public class PasswordHasherTests
{
    private readonly IPasswordHasher _hasher = new Pbkdf2PasswordHasher();

    [Fact]
    public void Hash_ShouldProduceVerifyableOutput()
    {
        var hash = _hasher.Hash("Admin@123");
        Assert.True(_hasher.Verify("Admin@123", hash));
    }

    [Fact]
    public void Verify_ShouldRejectWrongPassword()
    {
        var hash = _hasher.Hash("correct");
        Assert.False(_hasher.Verify("wrong", hash));
    }

    [Fact]
    public void Hash_ShouldProduceDifferentOutputForSamePassword()
    {
        var h1 = _hasher.Hash("password");
        var h2 = _hasher.Hash("password");
        Assert.NotEqual(h1, h2);
        Assert.True(_hasher.Verify("password", h1));
        Assert.True(_hasher.Verify("password", h2));
    }

    [Fact]
    public void Hash_ShouldStartWithAlgorithmPrefix()
    {
        var hash = _hasher.Hash("test");
        Assert.StartsWith("wecms.pbkdf2-sha256.", hash);
    }
}