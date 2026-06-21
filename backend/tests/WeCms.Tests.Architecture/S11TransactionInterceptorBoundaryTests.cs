namespace WeCms.Tests.Architecture;

public sealed class S11TransactionInterceptorBoundaryTests
{
    [Fact]
    public async Task TransactionInterceptor_DoesNotBlockSynchronously()
    {
        var source = await ReadTransactionInterceptorSourceAsync();

        Assert.DoesNotContain(".Wait(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Result", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WaitAll", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetAwaiter().GetResult", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TransactionInterceptor_DoesNotCreateDistributedTransactions()
    {
        var source = await ReadTransactionInterceptorSourceAsync();

        Assert.DoesNotContain("TransactionScope", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Transactions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CommittableTransaction", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DistributedTransaction", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TransactionInterceptor_DoesNotWireDynamicProxyOrAutofac()
    {
        var source = await ReadTransactionInterceptorSourceAsync();

        Assert.DoesNotContain("Autofac", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DynamicProxy", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Castle", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInterceptor", source, StringComparison.Ordinal);
    }

    private static Task<string> ReadTransactionInterceptorSourceAsync()
    {
        return File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Aop", "TransactionInterceptor.cs"),
            TestContext.Current.CancellationToken);
    }
}
