using System.Text;

namespace WeCms.Data.SqlSugar;

public sealed class MigrationScaffold
{
    public string CreateReviewableDiff(SqlSugarSchemaValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine("-- WeCMS schema validation diff");
        builder.AppendLine("-- Review this output before writing a migration.");

        foreach (var missingTable in result.MissingTables.OrderBy(item => item.TableName, StringComparer.Ordinal))
        {
            builder.AppendLine($"MISSING_TABLE {missingTable.TableName} model={missingTable.ModelType.FullName}");
        }

        foreach (var missingColumn in result.MissingColumns.OrderBy(item => item.TableName, StringComparer.Ordinal).ThenBy(item => item.ColumnName, StringComparer.Ordinal))
        {
            builder.AppendLine($"MISSING_COLUMN {missingColumn.TableName}.{missingColumn.ColumnName}");
        }

        foreach (var mismatch in result.NullableMismatches.OrderBy(item => item.TableName, StringComparer.Ordinal).ThenBy(item => item.ColumnName, StringComparer.Ordinal))
        {
            builder.AppendLine($"NULLABLE_MISMATCH {mismatch.TableName}.{mismatch.ColumnName} expected={mismatch.ExpectedNullable} actual={mismatch.ActualNullable}");
        }

        foreach (var mismatch in result.LengthMismatches.OrderBy(item => item.TableName, StringComparer.Ordinal).ThenBy(item => item.ColumnName, StringComparer.Ordinal))
        {
            builder.AppendLine($"LENGTH_MISMATCH {mismatch.TableName}.{mismatch.ColumnName} expected={mismatch.ExpectedMaxLength} actual={mismatch.ActualMaxLength}");
        }

        foreach (var mismatch in result.IndexMismatches.OrderBy(item => item.TableName, StringComparer.Ordinal).ThenBy(item => item.IndexName, StringComparer.Ordinal))
        {
            builder.AppendLine($"INDEX_MISMATCH {mismatch.TableName}.{mismatch.IndexName} {mismatch.Reason}");
        }

        return builder.ToString();
    }
}
