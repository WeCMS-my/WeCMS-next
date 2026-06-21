namespace WeCms.Data.SqlSugar;

public sealed class CodeFirstModelRegistry : ICodeFirstModelRegistry
{
    private readonly IReadOnlyCollection<ICodeFirstModelProvider> _providers;

    public CodeFirstModelRegistry(IEnumerable<ICodeFirstModelProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        _providers = providers.ToArray();
    }

    public IReadOnlyList<Type> GetModelTypes()
    {
        var modelTypes = new List<Type>();
        var seen = new HashSet<Type>();
        var tableNames = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in _providers)
        {
            foreach (var modelType in provider.GetModelTypes())
            {
                if (modelType is null)
                {
                    throw new InvalidOperationException("CodeFirst model providers must not return null model types.");
                }

                if (seen.Add(modelType))
                {
                    var tableName = ResolveTableName(modelType);
                    if (tableNames.TryGetValue(tableName, out var existingModelType))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate CodeFirst table name '{tableName}' declared by {existingModelType.FullName} and {modelType.FullName}.");
                    }

                    tableNames.Add(tableName, modelType);
                    modelTypes.Add(modelType);
                }
            }
        }

        return modelTypes;
    }

    private static string ResolveTableName(Type modelType)
    {
        foreach (var attribute in modelType.GetCustomAttributes(inherit: false))
        {
            var attributeType = attribute.GetType();
            if (attributeType.Name is not ("SugarTable" or "SugarTableAttribute"))
            {
                continue;
            }

            var tableName = attributeType.GetProperty("TableName")?.GetValue(attribute) as string;
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                return tableName;
            }
        }

        throw new InvalidOperationException($"CodeFirst model {modelType.FullName} must declare SugarTable.");
    }
}
