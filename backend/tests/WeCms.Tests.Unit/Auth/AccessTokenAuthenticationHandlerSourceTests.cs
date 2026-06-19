namespace WeCms.Tests.Unit.Auth;

public sealed class AccessTokenAuthenticationHandlerSourceTests
{
    [Fact]
    public async Task AccessTokenAuthenticationHandler_WritesApiResultForChallengeAndForbidden()
    {
        var source = await File.ReadAllTextAsync(RepoPath(
            "backend",
            "src",
            "WeCms.Modules.System",
            "Auth",
            "AccessTokenAuthenticationHandler.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("protected override Task HandleChallengeAsync", source, StringComparison.Ordinal);
        Assert.Contains("protected override Task HandleForbiddenAsync", source, StringComparison.Ordinal);
        Assert.Contains("ApiCodes.Unauthorized", source, StringComparison.Ordinal);
        Assert.Contains("ApiCodes.Forbidden", source, StringComparison.Ordinal);
        Assert.Contains("ApiResult<object>.Error(code, message, context.TraceIdentifier)", source, StringComparison.Ordinal);
        Assert.Contains("writer.WriteString(\"traceId\", result.TraceId);", source, StringComparison.Ordinal);
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
