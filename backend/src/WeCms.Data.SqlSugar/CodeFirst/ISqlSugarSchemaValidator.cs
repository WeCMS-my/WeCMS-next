namespace WeCms.Data.SqlSugar;

public interface ISqlSugarSchemaValidator
{
    Task<SqlSugarSchemaValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}
