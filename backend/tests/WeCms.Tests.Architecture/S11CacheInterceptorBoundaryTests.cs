namespace WeCms.Tests.Architecture;

public sealed class S11CacheInterceptorBoundaryTests
{
    [Fact]
    public async Task CacheInterceptor_DoesNotWireDynamicProxyOrAutofac()
    {
        var source = await ReadCacheInterceptorSourceAsync();

        Assert.DoesNotContain("Autofac", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DynamicProxy", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Castle", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInterceptor", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CacheInterceptor_DoesNotBlockSynchronously()
    {
        var source = await ReadCacheInterceptorSourceAsync();

        Assert.DoesNotContain(".Wait(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Result", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WaitAll", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetAwaiter().GetResult", source, StringComparison.Ordinal);
    }

    private static Task<string> ReadCacheInterceptorSourceAsync()
    {
        return File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Aop", "CacheInterceptor.cs"),
            TestContext.Current.CancellationToken);
    }
}
