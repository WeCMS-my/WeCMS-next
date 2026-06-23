using WeCms.Shared;

namespace WeCms.Tests.Unit.Auth;

public sealed partial class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_UnknownUserStillVerifiesDummyHash()
    {
        var repository = new FakeAuthRepository();
        var passwordHasher = new CountingPasswordHasher();
        var service = CreateService(repository, passwordHasher);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("missing", "wrong"), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal(1, passwordHasher.VerifyCalls);
        Assert.False(string.IsNullOrWhiteSpace(passwordHasher.LastPasswordHash));
    }

    [Fact]
    public async Task LoginAsync_DisabledUserStillVerifiesPasswordHash()
    {
        var disabledHash = PasswordHasher.HashForTest("correct");
        var repository = new FakeAuthRepository
        {
            User = new AuthUserRecord(1, "disabled", "Disabled", disabledHash, "disabled", false)
        };
        var passwordHasher = new CountingPasswordHasher();
        var service = CreateService(repository, passwordHasher);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("disabled", "wrong"), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal(1, passwordHasher.VerifyCalls);
        Assert.Equal(disabledHash, passwordHasher.LastPasswordHash);
    }
}
