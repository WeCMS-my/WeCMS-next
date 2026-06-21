using System.Globalization;
using System.Reflection;
using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed class SqlSugarSchemaValidator : ISqlSugarSchemaValidator
{
    private readonly ICodeFirstModelRegistry _modelRegistry;
    private readonly Func<string, CancellationToken, Task<SqlSugarTableSchema?>> _loadTableSchema;
    private readonly NullabilityInfoContext _nullabilityInfoContext = new();

    public SqlSugarSchemaValidator(ICodeFirstModelRegistry modelRegistry, ISqlSugarClient db)
        : this(modelRegistry, (tableName, _) =>
        {
            ArgumentNullException.ThrowIfNull(db);

            var columns = db.Ado.SqlQuery<InformationSchemaColumn>(
                """
                SELECT
                    column_name AS ColumnName,
                    is_nullable AS IsNullable,
                    character_maximum_length AS CharacterMaximumLength
                FROM information_schema.columns
                WHERE table_schema = DATABASE()
                  AND table_name = @tableName
                """,
                new SugarParameter("@tableName", tableName));

            if (columns.Count == 0)
            {
                return Task.FromResult<SqlSugarTableSchema?>(null);
            }

            var indexes = db.Ado.SqlQuery<InformationSchemaIndex>(
                """
                SELECT
                    index_name AS IndexName,
                    MIN(non_unique) AS NonUnique
                FROM information_schema.statistics
                WHERE table_schema = DATABASE()
                  AND table_name = @tableName
                GROUP BY index_name
                """,
                new SugarParameter("@tableName", tableName));

            return Task.FromResult<SqlSugarTableSchema?>(
                new SqlSugarTableSchema(
                    tableName,
                    columns.Select(column => new SqlSugarColumnSchema(
                        column.ColumnName,
                        string.Equals(column.IsNullable, "YES", StringComparison.OrdinalIgnoreCase),
                        column.CharacterMaximumLength)).ToArray(),
                    indexes
                        .Where(index => !string.Equals(index.IndexName, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                        .Select(index => new SqlSugarIndexSchema(index.IndexName, index.NonUnique == 0))
                        .ToArray()));
        })
    {
    }

    public SqlSugarSchemaValidator(
        ICodeFirstModelRegistry modelRegistry,
        Func<string, CancellationToken, Task<bool>> tableExists)
        : this(
            modelRegistry,
            async (tableName, cancellationToken) =>
                await tableExists(tableName, cancellationToken).ConfigureAwait(false)
                    ? new SqlSugarTableSchema(tableName, [], [])
                    : null)
    {
        ArgumentNullException.ThrowIfNull(tableExists);
    }

    public SqlSugarSchemaValidator(
        ICodeFirstModelRegistry modelRegistry,
        Func<string, CancellationToken, Task<SqlSugarTableSchema?>> loadTableSchema)
    {
        _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
        _loadTableSchema = loadTableSchema ?? throw new ArgumentNullException(nameof(loadTableSchema));
    }

    public async Task<SqlSugarSchemaValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var missingTables = new List<SqlSugarMissingTable>();
        var missingColumns = new List<SqlSugarMissingColumn>();
        var nullableMismatches = new List<SqlSugarNullableMismatch>();
        var lengthMismatches = new List<SqlSugarLengthMismatch>();
        var indexMismatches = new List<SqlSugarIndexMismatch>();

        foreach (var modelType in _modelRegistry.GetModelTypes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tableName = ResolveTableName(modelType);
            var tableSchema = await _loadTableSchema(tableName, cancellationToken).ConfigureAwait(false);
            if (tableSchema is null)
            {
                missingTables.Add(new SqlSugarMissingTable(modelType, tableName));
                continue;
            }

            ValidateColumns(modelType, tableName, tableSchema, missingColumns, nullableMismatches, lengthMismatches);
            ValidateIndexes(modelType, tableName, tableSchema, indexMismatches);
        }

        return new SqlSugarSchemaValidationResult(
            missingTables,
            missingColumns,
            nullableMismatches,
            lengthMismatches,
            indexMismatches);
    }

    private void ValidateColumns(
        Type modelType,
        string tableName,
        SqlSugarTableSchema tableSchema,
        List<SqlSugarMissingColumn> missingColumns,
        List<SqlSugarNullableMismatch> nullableMismatches,
        List<SqlSugarLengthMismatch> lengthMismatches)
    {
        var actualColumns = tableSchema.Columns.ToDictionary(
            column => column.ColumnName,
            StringComparer.OrdinalIgnoreCase);

        foreach (var expectedColumn in ResolveColumns(modelType))
        {
            if (!actualColumns.TryGetValue(expectedColumn.ColumnName, out var actualColumn))
            {
                missingColumns.Add(new SqlSugarMissingColumn(tableName, expectedColumn.ColumnName));
                continue;
            }

            if (expectedColumn.IsNullable != actualColumn.IsNullable)
            {
                nullableMismatches.Add(new SqlSugarNullableMismatch(
                    tableName,
                    expectedColumn.ColumnName,
                    expectedColumn.IsNullable,
                    actualColumn.IsNullable));
            }

            if (expectedColumn.MaxLength is not null && expectedColumn.MaxLength != actualColumn.MaxLength)
            {
                lengthMismatches.Add(new SqlSugarLengthMismatch(
                    tableName,
                    expectedColumn.ColumnName,
                    expectedColumn.MaxLength.Value,
                    actualColumn.MaxLength));
            }
        }
    }

    private static void ValidateIndexes(
        Type modelType,
        string tableName,
        SqlSugarTableSchema tableSchema,
        List<SqlSugarIndexMismatch> indexMismatches)
    {
        var actualIndexes = tableSchema.Indexes.ToDictionary(
            index => index.IndexName,
            StringComparer.OrdinalIgnoreCase);

        foreach (var expectedIndex in ResolveIndexes(modelType))
        {
            if (!actualIndexes.TryGetValue(expectedIndex.IndexName, out var actualIndex))
            {
                indexMismatches.Add(new SqlSugarIndexMismatch(tableName, expectedIndex.IndexName, "missing index"));
                continue;
            }

            if (expectedIndex.IsUnique != actualIndex.IsUnique)
            {
                indexMismatches.Add(new SqlSugarIndexMismatch(tableName, expectedIndex.IndexName, "unique mismatch"));
            }
        }
    }

    private IReadOnlyList<SqlSugarColumnSchema> ResolveColumns(Type modelType)
    {
        return modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => ResolveColumn(property))
            .Where(column => column is not null)
            .Select(column => column!)
            .ToArray();
    }

    private SqlSugarColumnSchema? ResolveColumn(PropertyInfo property)
    {
        var attribute = property.GetCustomAttributes(inherit: true)
            .FirstOrDefault(candidate => candidate.GetType().Name is "SugarColumn" or "SugarColumnAttribute");
        if (attribute is not null && ReadBool(attribute, "IsIgnore"))
        {
            return null;
        }

        var columnName = ReadString(attribute, "ColumnName");
        if (string.IsNullOrWhiteSpace(columnName))
        {
            columnName = ToSnakeCase(property.Name);
        }

        var length = ReadInt(attribute, "Length");
        return new SqlSugarColumnSchema(
            columnName,
            IsNullable(property, attribute),
            length > 0 ? length : null);
    }

    private bool IsNullable(PropertyInfo property, object? sugarColumnAttribute)
    {
        if (ReadBool(sugarColumnAttribute, "IsNullable"))
        {
            return true;
        }

        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
        {
            return true;
        }

        if (!property.PropertyType.IsValueType)
        {
            return _nullabilityInfoContext.Create(property).ReadState == NullabilityState.Nullable;
        }

        return false;
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

    private static IReadOnlyList<SqlSugarIndexSchema> ResolveIndexes(Type modelType)
    {
        return modelType.GetCustomAttributes(inherit: false)
            .Where(attribute => attribute.GetType().Name is "SugarIndex" or "SugarIndexAttribute")
            .Select(attribute => new SqlSugarIndexSchema(
                ReadRequiredString(attribute, "IndexName"),
                ReadBool(attribute, "IsUnique")))
            .ToArray();
    }

    private static string ReadRequiredString(object target, string propertyName)
    {
        var value = ReadString(target, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} is required.");
        }

        return value;
    }

    private static string ReadString(object? target, string propertyName)
    {
        return target?.GetType().GetProperty(propertyName)?.GetValue(target) as string ?? string.Empty;
    }

    private static bool ReadBool(object? target, string propertyName)
    {
        return target is not null
            && target.GetType().GetProperty(propertyName)?.GetValue(target) is true;
    }

    private static int ReadInt(object? target, string propertyName)
    {
        var value = target?.GetType().GetProperty(propertyName)?.GetValue(target);
        return value is null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static string ToSnakeCase(string value)
    {
        var chars = new List<char>(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (char.IsUpper(current) && index > 0)
            {
                chars.Add('_');
            }

            chars.Add(char.ToLowerInvariant(current));
        }

        return new string(chars.ToArray());
    }

    public sealed class InformationSchemaColumn
    {
        public string ColumnName { get; set; } = string.Empty;

        public string IsNullable { get; set; } = string.Empty;

        public int? CharacterMaximumLength { get; set; }
    }

    public sealed class InformationSchemaIndex
    {
        public string IndexName { get; set; } = string.Empty;

        public int NonUnique { get; set; }
    }
}
