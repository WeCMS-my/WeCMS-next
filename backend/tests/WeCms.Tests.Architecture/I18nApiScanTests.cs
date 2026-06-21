namespace WeCms.Tests.Architecture;

public sealed class I18nApiScanTests
{
    [Fact]
    public async Task I18nEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Configuration", "I18n", "I18nEndpoints.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGroup(\"/api/v1/system/i18n/messages\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("systemGroup.MapGet(\"\", ListAsync).RequireEndpointPermission(I18nPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("systemGroup.MapGet(\"/{id:long}\", DetailAsync).RequireEndpointPermission(I18nPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("systemGroup.MapPost(\"\", CreateAsync).RequireEndpointPermission(I18nPermissions.Create).RequireRateLimiting(AdminWriteRateLimitPolicy)", source, StringComparison.Ordinal);
        Assert.Contains("systemGroup.MapPut(\"/{id:long}\", UpdateAsync).RequireEndpointPermission(I18nPermissions.Update).RequireRateLimiting(AdminWriteRateLimitPolicy)", source, StringComparison.Ordinal);
        Assert.Contains("systemGroup.MapDelete(\"/{id:long}\", DeleteAsync).RequireEndpointPermission(I18nPermissions.Delete).RequireRateLimiting(AdminWriteRateLimitPolicy)", source, StringComparison.Ordinal);
        Assert.Contains("endpoints.MapGet(\"/api/v1/i18n/messages\", PublicMessagesAsync)", source, StringComparison.Ordinal);
        Assert.Contains("endpoints.MapPost(\"/api/v1/account/i18n/switch\", SwitchLocaleAsync)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(I18nPermissions.AccountSwitch)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersI18nEndpointsThroughConfiguration()
    {
        var programSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);
        var endpointMapSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);
        var configurationSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Configuration", "ConfigurationServiceCollectionExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsConfiguration();", programSource, StringComparison.Ordinal);
        Assert.Contains("endpoints.MapI18nEndpoints();", endpointMapSource, StringComparison.Ordinal);
        Assert.Contains("services.AddWeCmsConfigurationI18n();", configurationSource, StringComparison.Ordinal);
    }
}
