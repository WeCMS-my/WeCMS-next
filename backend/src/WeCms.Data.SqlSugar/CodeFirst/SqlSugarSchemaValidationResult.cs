namespace WeCms.Data.SqlSugar;

public sealed record SqlSugarSchemaValidationResult(
    IReadOnlyList<SqlSugarMissingTable> MissingTables,
    IReadOnlyList<SqlSugarMissingColumn> MissingColumns,
    IReadOnlyList<SqlSugarNullableMismatch> NullableMismatches,
    IReadOnlyList<SqlSugarLengthMismatch> LengthMismatches,
    IReadOnlyList<SqlSugarIndexMismatch> IndexMismatches)
{
    public SqlSugarSchemaValidationResult(IReadOnlyList<SqlSugarMissingTable> missingTables)
        : this(missingTables, [], [], [], [])
    {
    }

    public bool IsValid => MissingTables.Count == 0
        && MissingColumns.Count == 0
        && NullableMismatches.Count == 0
        && LengthMismatches.Count == 0
        && IndexMismatches.Count == 0;
}
