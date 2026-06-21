using SqlSugar;
using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class SqlSugarCodeFirstRunnerTests
{
    [Fact]
    public async Task CodeFirstRunner_FailsInProductionLikeMode()
    {
        var initialized = false;
        var runner = new SqlSugarCodeFirstRunner(
            new CodeFirstModelRegistry([new TestModelProvider(typeof(CodeFirstProbeEntity))]),
            _ => initialized = true,
            "Production");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.InitializeDevelopmentAsync(TestContext.Current.CancellationToken));

        Assert.Equal("CodeFirst initialization is only allowed in Development or Testing environments.", exception.Message);
        Assert.False(initialized);
    }

    [Fact]
    public void CodeFirstRunner_ImplementsCodeFirstRunnerAbstraction()
    {
        var runner = new SqlSugarCodeFirstRunner(
            new CodeFirstModelRegistry([]),
            _ => { },
            "Development");

        Assert.IsAssignableFrom<ICodeFirstRunner>(runner);
    }

    [Fact]
    public void CodeFirstRunner_CollectsModelsFromProviders()
    {
        var runner = new SqlSugarCodeFirstRunner(
            new CodeFirstModelRegistry(
            [
                new TestModelProvider(typeof(CodeFirstProbeEntity)),
                new TestModelProvider(typeof(CodeFirstAuditEntity), typeof(CodeFirstProbeEntity))
            ]),
            _ => { },
            "Development");

        var modelTypes = runner.CollectModelTypes();

        Assert.Equal([typeof(CodeFirstProbeEntity), typeof(CodeFirstAuditEntity)], modelTypes);
    }

    [Fact]
    public void CodeFirstModelRegistry_FailsOnDuplicateTable()
    {
        var registry = new CodeFirstModelRegistry(
        [
            new TestModelProvider(typeof(CodeFirstProbeEntity)),
            new TestModelProvider(typeof(DuplicateCodeFirstProbeEntity))
        ]);

        var exception = Assert.Throws<InvalidOperationException>(registry.GetModelTypes);

        Assert.Contains("Duplicate CodeFirst table name", exception.Message, StringComparison.Ordinal);
        Assert.Contains("s3_codefirst_probe", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CodeFirstModelRegistry_FailsOnMissingSugarTable()
    {
        var registry = new CodeFirstModelRegistry([new TestModelProvider(typeof(MissingSugarTableEntity))]);

        var exception = Assert.Throws<InvalidOperationException>(registry.GetModelTypes);

        Assert.Contains("must declare SugarTable", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CodeFirstRunner_ValidatesRegisteredModelsInDevelopmentOrTestOnly()
    {
        IReadOnlyCollection<Type>? initializedTypes = null;
        var runner = new SqlSugarCodeFirstRunner(
            new CodeFirstModelRegistry([new TestModelProvider(typeof(CodeFirstProbeEntity))]),
            modelTypes => initializedTypes = modelTypes,
            "Testing");

        await runner.InitializeDevelopmentAsync(TestContext.Current.CancellationToken);

        Assert.Equal([typeof(CodeFirstProbeEntity)], initializedTypes);
    }

    [Fact]
    public async Task ModuleSqlSugarExtensions_RegisterCodeFirstProviders()
    {
        var moduleExtensions = new[]
        {
            ("AccessControl", "AccessControlSqlSugarServiceCollectionExtensions.cs"),
            ("Audit", "AuditSqlSugarServiceCollectionExtensions.cs"),
            ("Configuration", "ConfigurationSqlSugarServiceCollectionExtensions.cs"),
            ("FileCenter", "FileCenterSqlSugarServiceCollectionExtensions.cs"),
            ("Identity", "IdentitySqlSugarServiceCollectionExtensions.cs"),
            ("Organization", "OrganizationSqlSugarServiceCollectionExtensions.cs"),
            ("Platform", "PlatformSqlSugarServiceCollectionExtensions.cs"),
            ("Security", "SecuritySqlSugarServiceCollectionExtensions.cs")
        };

        foreach (var (moduleName, fileName) in moduleExtensions)
        {
            var source = await File.ReadAllTextAsync(
                RepoPath("backend", "src", $"WeCms.Modules.{moduleName}.SqlSugar", fileName),
                TestContext.Current.CancellationToken);

            Assert.Contains("ICodeFirstModelProvider", source, StringComparison.Ordinal);
            Assert.Contains("AddSingleton<ICodeFirstModelProvider", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ApiProgram_RegistersModuleSqlSugarAdapters()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("codeFirstEnvironmentName: builder.Environment.EnvironmentName", source, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsAccessControlSqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsAuditSqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsConfigurationSqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsFileCenterSqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsIdentitySqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsOrganizationSqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsPlatformSqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsSecuritySqlSugar", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SqlSugarDataRegistration_RegistersCodeFirstPlatformServices()
    {
        var source = await File.ReadAllTextAsync(
            RepoPath("backend", "src", "WeCms.Data.SqlSugar", "SqlSugarDataServiceCollectionExtensions.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("codeFirstEnvironmentName", source, StringComparison.Ordinal);
        Assert.Contains("AddScoped<ICodeFirstModelRegistry", source, StringComparison.Ordinal);
        Assert.Contains("AddScoped<ICodeFirstRunner", source, StringComparison.Ordinal);
        Assert.Contains("AddScoped<ISqlSugarSchemaValidator", source, StringComparison.Ordinal);
    }

    private sealed class TestModelProvider : ICodeFirstModelProvider
    {
        private readonly IReadOnlyList<Type> _modelTypes;

        public TestModelProvider(params Type[] modelTypes)
        {
            _modelTypes = modelTypes;
        }

        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return _modelTypes;
        }
    }

    [SugarTable("s3_codefirst_probe")]
    private sealed class CodeFirstProbeEntity
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }
    }

    [SugarTable("s3_codefirst_audit")]
    private sealed class CodeFirstAuditEntity
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }
    }

    [SugarTable("s3_codefirst_probe")]
    private sealed class DuplicateCodeFirstProbeEntity
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }
    }

    private sealed class MissingSugarTableEntity
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }
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
