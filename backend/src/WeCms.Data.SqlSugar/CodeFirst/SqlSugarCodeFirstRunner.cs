using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed class SqlSugarCodeFirstRunner : ICodeFirstRunner
{
    private const string ProductionProtectionMessage = "CodeFirst initialization is only allowed in Development or Testing environments.";
    private readonly ICodeFirstModelRegistry _modelRegistry;
    private readonly Action<IReadOnlyCollection<Type>> _initializeTables;
    private readonly string _environmentName;

    public SqlSugarCodeFirstRunner(
        ICodeFirstModelRegistry modelRegistry,
        ISqlSugarClient db,
        string environmentName)
        : this(modelRegistry, modelTypes => db.CodeFirst.InitTables(modelTypes.ToArray()), environmentName)
    {
        ArgumentNullException.ThrowIfNull(db);
    }

    public SqlSugarCodeFirstRunner(
        ICodeFirstModelRegistry modelRegistry,
        Action<IReadOnlyCollection<Type>> initializeTables,
        string environmentName)
    {
        _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
        _initializeTables = initializeTables ?? throw new ArgumentNullException(nameof(initializeTables));
        _environmentName = string.IsNullOrWhiteSpace(environmentName)
            ? throw new ArgumentException("CodeFirst environment name is required.", nameof(environmentName))
            : environmentName;
    }

    public IReadOnlyList<Type> CollectModelTypes()
    {
        return _modelRegistry.GetModelTypes();
    }

    public Task InitializeDevelopmentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsDevelopmentLike(_environmentName))
        {
            throw new InvalidOperationException(ProductionProtectionMessage);
        }

        _initializeTables(CollectModelTypes());

        return Task.CompletedTask;
    }

    private static bool IsDevelopmentLike(string environmentName)
    {
        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
    }
}
