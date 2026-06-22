using System.Collections.Generic;
using System.Linq;
using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Data.SqlSugar;

public sealed class SqlSugarSqlAuditRegistrar : ISqlSugarAuditRegistrar
{
    private readonly SqlAuditRecorder _recorder;
    private readonly IReadOnlySet<string> _softDeleteTables;
    private readonly IReadOnlySet<string> _tenantTables;
    private readonly IReadOnlySet<string> _dataScopeTables;
    private readonly IQueryFilterContextAccessor _contextAccessor;
    private readonly QueryFilterBypass _queryFilterBypass;
    private readonly IReadOnlyList<ISqlTimingRecorder> _timingRecorders;
    private readonly SqlAuditRedactor _redactor = new();

    public SqlSugarSqlAuditRegistrar(
        SqlAuditRecorder recorder,
        ICodeFirstModelRegistry codeFirstModelRegistry,
        IQueryFilterContextAccessor contextAccessor,
        QueryFilterBypass queryFilterBypass,
        IEnumerable<ISqlTimingRecorder>? timingRecorders = null)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _softDeleteTables = BuildSoftDeleteTableNames(codeFirstModelRegistry);
        _tenantTables = BuildTableNamesByInterface(codeFirstModelRegistry, typeof(ITenantEntity));
        _dataScopeTables = BuildTableNamesByInterface(codeFirstModelRegistry, typeof(IDataScopedEntity));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _queryFilterBypass = queryFilterBypass ?? throw new ArgumentNullException(nameof(queryFilterBypass));
        _timingRecorders = (timingRecorders ?? []).ToArray();
    }

    public void Register(SqlSugarScopeProvider db)
    {
        ArgumentNullException.ThrowIfNull(db);

        var connectionName = db.CurrentConnectionConfig.ConfigId?.ToString() ?? "default";

        db.Aop.OnLogExecuting = (sql, _) =>
        {
            if (_queryFilterBypass.IsBypassed)
            {
                return;
            }

            RawSqlFilterGuard.RequireDataBoundaryFilters(
                sql,
                nameof(SqlSugarSqlAuditRegistrar),
                _contextAccessor.Current,
                _softDeleteTables,
                _tenantTables,
                _dataScopeTables);
        };

        db.Aop.OnLogExecuted = (sql, parameters) =>
        {
            var sugarParameters = ToSugarParameters(parameters);
            _recorder.RecordExecuted(
                connectionName,
                sql,
                sugarParameters,
                db.Ado.SqlExecutionTime);
            RecordSqlTiming(connectionName, sql, sugarParameters, db.Ado.SqlExecutionTime, exception: null);
        };

        db.Aop.OnError = exception =>
        {
            var sugarParameters = ToSugarParameters(exception.Parametres);
            _recorder.RecordFailed(
                connectionName,
                exception.Sql,
                sugarParameters,
                exception);
            RecordSqlTiming(connectionName, exception.Sql, sugarParameters, TimeSpan.Zero, exception);
        };
    }

    private static IReadOnlySet<string> BuildSoftDeleteTableNames(ICodeFirstModelRegistry codeFirstModelRegistry)
    {
        ArgumentNullException.ThrowIfNull(codeFirstModelRegistry);

        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var modelType in codeFirstModelRegistry.GetModelTypes())
        {
            if (!typeof(ISoftDeleteEntity).IsAssignableFrom(modelType))
            {
                continue;
            }

            tableNames.Add(ResolveTableName(modelType));
        }

        return tableNames;
    }

    private static IReadOnlySet<string> BuildTableNamesByInterface(
        ICodeFirstModelRegistry codeFirstModelRegistry,
        Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(interfaceType);

        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var modelType in codeFirstModelRegistry.GetModelTypes())
        {
            if (!interfaceType.IsAssignableFrom(modelType))
            {
                continue;
            }

            tableNames.Add(ResolveTableName(modelType));
        }

        return tableNames;
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

    private void RecordSqlTiming(
        string connectionName,
        string sql,
        IReadOnlyList<SugarParameter> parameters,
        TimeSpan elapsed,
        Exception? exception)
    {
        if (_timingRecorders.Count == 0)
        {
            return;
        }

        var record = new SqlTimingRecord(
            connectionName,
            OperationType(sql),
            sql,
            _redactor.Redact(parameters),
            elapsed);
        foreach (var timingRecorder in _timingRecorders)
        {
            if (exception is null)
            {
                timingRecorder.RecordExecuted(record);
            }
            else
            {
                timingRecorder.RecordFailed(record, exception);
            }
        }
    }

    private static IReadOnlyList<SugarParameter> ToSugarParameters(object? parameters)
    {
        return parameters switch
        {
            null => [],
            SugarParameter[] sugarParameters => sugarParameters,
            IReadOnlyList<SugarParameter> sugarParameters => sugarParameters,
            IEnumerable<SugarParameter> sugarParameters => sugarParameters.ToArray(),
            _ => []
        };
    }

    private static string OperationType(string sql)
    {
        var trimmed = sql.TrimStart();
        if (trimmed.Length == 0)
        {
            return "UNKNOWN";
        }

        var firstWhitespace = trimmed.IndexOfAny([' ', '\r', '\n', '\t']);
        var operation = firstWhitespace < 0 ? trimmed : trimmed[..firstWhitespace];
        return operation.ToUpperInvariant();
    }
}
