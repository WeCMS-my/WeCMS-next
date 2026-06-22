namespace WeCms.Tests.Architecture;

public sealed class S12EventBusBoundaryTests
{
    [Fact]
    public async Task EventBusAbstractions_DoNotReferenceDataInfrastructure()
    {
        var root = FindRepositoryRoot();
        var eventBusRoot = Path.Combine(root, "backend", "src", "WeCms.EventBus");
        var sources = Directory.GetFiles(eventBusRoot, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText);
        var combined = string.Join(Environment.NewLine, sources);

        Assert.DoesNotContain("SqlSugar", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("MySqlConnector", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbConnection", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbTransaction", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT ", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EventBus_DoesNotScanEndpointsAtRuntime()
    {
        var root = FindRepositoryRoot();
        var eventBusRoot = Path.Combine(root, "backend", "src", "WeCms.EventBus");
        var sources = Directory.GetFiles(eventBusRoot, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText);
        var combined = string.Join(Environment.NewLine, sources);

        Assert.DoesNotContain("EndpointDataSource", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("RouteEndpoint", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("MapEndpoint", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", combined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OutboxSqlSugarAdapter_IsRegisteredWithoutDistributedTransactions()
    {
        var root = FindRepositoryRoot();
        var adapterRoot = Path.Combine(root, "backend", "src", "WeCms.EventBus.SqlSugar");
        var extensionPath = Path.Combine(adapterRoot, "WeCmsEventBusSqlSugarServiceCollectionExtensions.cs");
        var projectPath = Path.Combine(adapterRoot, "WeCms.EventBus.SqlSugar.csproj");
        var apiProgram = await File.ReadAllTextAsync(
            Path.Combine(root, "backend", "src", "WeCms.Api", "Program.cs"),
            TestContext.Current.CancellationToken);

        Assert.True(File.Exists(projectPath), $"Missing project: {projectPath}");
        Assert.True(File.Exists(extensionPath), $"Missing extension: {extensionPath}");

        var source = string.Join(
            Environment.NewLine,
            Directory.GetFiles(adapterRoot, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.Contains("AddWeCmsEventBus", apiProgram, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsEventBusSqlSugar", apiProgram, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsCaching", apiProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("NoopAuditWriter", apiProgram, StringComparison.Ordinal);
        Assert.Contains("ICodeFirstModelProvider", source, StringComparison.Ordinal);
        Assert.Contains("IOutboxMessageRepository", source, StringComparison.Ordinal);
        Assert.Contains("IOutboxWriter", source, StringComparison.Ordinal);
        Assert.Contains("AddHostedService<OutboxDispatcherHostedService>", source, StringComparison.Ordinal);
        Assert.Contains("BackgroundService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TransactionScope", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Transactions", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OutboxMigration_DefinesExpectedSchema()
    {
        var root = FindRepositoryRoot();
        var migration = await File.ReadAllTextAsync(
            Path.Combine(root, "database", "migrations", "000001_baseline_system_schema.sql"),
            TestContext.Current.CancellationToken);

        Assert.Contains("CREATE TABLE sys_outbox_message", migration, StringComparison.Ordinal);
        Assert.Contains("event_id", migration, StringComparison.Ordinal);
        Assert.Contains("event_type", migration, StringComparison.Ordinal);
        Assert.Contains("aggregate_type", migration, StringComparison.Ordinal);
        Assert.Contains("aggregate_id", migration, StringComparison.Ordinal);
        Assert.Contains("payload_json", migration, StringComparison.Ordinal);
        Assert.Contains("retry_count", migration, StringComparison.Ordinal);
        Assert.Contains("available_at", migration, StringComparison.Ordinal);
        Assert.Contains("locked_at", migration, StringComparison.Ordinal);
        Assert.Contains("lock_token", migration, StringComparison.Ordinal);
        Assert.Contains("processed_at", migration, StringComparison.Ordinal);
        Assert.Contains("ix_sys_outbox_message_status_available", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SystemFoundationEvents_ArePublishedByApplicationServices()
    {
        var root = FindRepositoryRoot();
        var expectations = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["backend/src/WeCms.Modules.Identity/Services/UserService.cs"] = "UserCreatedEvent",
            ["backend/src/WeCms.Modules.Identity/Services/UserService.cs#disabled"] = "UserDisabledEvent",
            ["backend/src/WeCms.Modules.AccessControl/Roles/RoleService.cs"] = "RolePermissionsChangedEvent",
            ["backend/src/WeCms.Modules.AccessControl/Menus/MenuService.cs"] = "MenuChangedEvent",
            ["backend/src/WeCms.Modules.Configuration/Settings/SettingService.cs"] = "SettingChangedEvent",
            ["backend/src/WeCms.Modules.Configuration/Dicts/DictService.cs"] = "DictChangedEvent",
            ["backend/src/WeCms.Modules.Configuration/I18n/I18nMessageService.cs"] = "I18nChangedEvent",
            ["backend/src/WeCms.Modules.Security/SecurityBanService.cs"] = "SecurityBanCreatedEvent"
        };

        foreach (var (relativePath, eventName) in expectations)
        {
            var path = relativePath.Split('#')[0];
            var source = await File.ReadAllTextAsync(Path.Combine(root, path), TestContext.Current.CancellationToken);
            Assert.Contains("WriteAsync(", source, StringComparison.Ordinal);
            Assert.Contains(eventName, source, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "backend", "WeCms.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
