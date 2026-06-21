using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed class DbMigrationRunner : IDbMigrationRunner
{
    private const string EnsureMigrationTableSql = """
        CREATE TABLE IF NOT EXISTS sys_schema_migration (
          version VARCHAR(64) NOT NULL,
          checksum CHAR(64) NOT NULL,
          applied_at DATETIME(6) NOT NULL,
          PRIMARY KEY (version)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """;

    private readonly ISqlSugarClient _db;

    public DbMigrationRunner(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<string>> MigrateAsync(string migrationsDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationsDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(migrationsDirectory))
        {
            throw new DirectoryNotFoundException($"Migration directory does not exist: {migrationsDirectory}");
        }

        _db.Ado.ExecuteCommand(EnsureMigrationTableSql);

        var appliedVersions = LoadAppliedMigrations();
        var appliedNow = new List<string>();

        foreach (var file in Directory.EnumerateFiles(migrationsDirectory, "*.sql").Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var version = Path.GetFileNameWithoutExtension(file);
            var sql = File.ReadAllText(file);
            var checksum = Sha256(sql);

            if (appliedVersions.TryGetValue(version, out var existingChecksum))
            {
                if (!string.Equals(existingChecksum, checksum, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Migration checksum drift detected for {version}.");
                }

                continue;
            }

            EnsureNoUntrackedCreatedTables(version, sql);

            _db.Ado.BeginTran();
            try
            {
                foreach (var statement in SplitSqlStatements(sql))
                {
                    _db.Ado.ExecuteCommand(statement);
                }

                _db.Ado.ExecuteCommand(
                    "INSERT INTO sys_schema_migration (version, checksum, applied_at) VALUES (@version, @checksum, UTC_TIMESTAMP(6))",
                    new SugarParameter("@version", version),
                    new SugarParameter("@checksum", checksum));

                _db.Ado.CommitTran();
                appliedNow.Add(version);
            }
            catch
            {
                _db.Ado.RollbackTran();
                throw;
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(appliedNow);
    }

    internal static IReadOnlyList<string> SplitSqlStatements(string sql)
    {
        var statements = new List<string>();
        if (string.IsNullOrWhiteSpace(sql))
        {
            return statements;
        }

        var statementBuilder = new StringBuilder();
        var beginDepth = 0;
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var inLineComment = false;
        var inBlockComment = false;

        for (var index = 0; index < sql.Length; index++)
        {
            var current = sql[index];

            if (inLineComment)
            {
                if (current == '\n')
                {
                    inLineComment = false;
                }

                continue;
            }

            if (inBlockComment)
            {
                if (current == '*' && index + 1 < sql.Length && sql[index + 1] == '/')
                {
                    inBlockComment = false;
                    index++;
                }

                continue;
            }

            if (inSingleQuote)
            {
                statementBuilder.Append(current);
                if (current == '\'')
                {
                    var escaped = index + 1 < sql.Length && sql[index + 1] == '\'';
                    if (escaped)
                    {
                        statementBuilder.Append(sql[index + 1]);
                        index++;
                    }
                    else
                    {
                        inSingleQuote = false;
                    }
                }

                continue;
            }

            if (inDoubleQuote)
            {
                statementBuilder.Append(current);
                if (current == '"')
                {
                    var escaped = index + 1 < sql.Length && sql[index + 1] == '"';
                    if (escaped)
                    {
                        statementBuilder.Append(sql[index + 1]);
                        index++;
                    }
                    else
                    {
                        inDoubleQuote = false;
                    }
                }

                continue;
            }

            if (current == '-' && index + 1 < sql.Length && sql[index + 1] == '-')
            {
                inLineComment = true;
                index++;
                continue;
            }

            if (current == '/' && index + 1 < sql.Length && sql[index + 1] == '*')
            {
                inBlockComment = true;
                index++;
                continue;
            }

            if (current == '\'')
            {
                inSingleQuote = true;
                statementBuilder.Append(current);
                continue;
            }

            if (current == '"')
            {
                inDoubleQuote = true;
                statementBuilder.Append(current);
                continue;
            }

            if (current == ';')
            {
                statementBuilder.Append(current);
                if (beginDepth == 0)
                {
                    var statement = statementBuilder.ToString().Trim();
                    if (statement.Length > 1)
                    {
                        statements.Add(statement.TrimEnd(';').Trim());
                    }

                    statementBuilder.Clear();
                }

                continue;
            }

            if (IsKeyword(sql, index, "BEGIN"))
            {
                beginDepth++;
            }
            else if (IsKeyword(sql, index, "END"))
            {
                if (beginDepth > 0)
                {
                    beginDepth--;
                }
            }

            statementBuilder.Append(current);
        }

        var trailingStatement = statementBuilder.ToString().Trim();
        if (trailingStatement.Length > 0)
        {
            statements.Add(trailingStatement.TrimEnd(';').Trim());
        }

        return statements;
    }

    private static bool IsKeyword(string sql, int index, string keyword)
    {
        if (!sql.AsSpan(index).StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var beforeIndex = index - 1;
        var afterIndex = index + keyword.Length;
        var beforeChar = beforeIndex >= 0 ? sql[beforeIndex] : ' ';
        var afterChar = afterIndex < sql.Length ? sql[afterIndex] : ' ';

        return IsWordBoundary(beforeChar) && IsWordBoundary(afterChar);
    }

    private static bool IsWordBoundary(char value)
    {
        return !char.IsLetterOrDigit(value) && value != '_';
    }

    private Dictionary<string, string> LoadAppliedMigrations()
    {
        return _db.Ado.SqlQuery<SchemaMigrationRecord>(
                "SELECT version AS Version, checksum AS Checksum FROM sys_schema_migration")
            .ToDictionary(row => row.Version, row => row.Checksum, StringComparer.Ordinal);
    }

    private void EnsureNoUntrackedCreatedTables(string version, string sql)
    {
        foreach (var tableName in ExtractCreatedTableNames(sql))
        {
            if (string.Equals(tableName, "sys_schema_migration", StringComparison.Ordinal))
            {
                continue;
            }

            var count = _db.Ado.GetScalar(
                "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @tableName",
                new SugarParameter("@tableName", tableName));

            if (Convert.ToInt32(count, CultureInfo.InvariantCulture) > 0)
            {
                throw new InvalidOperationException(
                    $"Migration {version} would create existing untracked table {tableName}.");
            }
        }
    }

    internal static IReadOnlyList<string> ExtractCreatedTableNames(string sql)
    {
        var tableNames = new List<string>();

        foreach (var rawLine in sql.Split('\n'))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var tableTokenIndex = tokens.Length > 4
                && string.Equals(tokens[2], "IF", StringComparison.OrdinalIgnoreCase)
                && string.Equals(tokens[3], "NOT", StringComparison.OrdinalIgnoreCase)
                && string.Equals(tokens[4], "EXISTS", StringComparison.OrdinalIgnoreCase)
                    ? 5
                    : 2;

            if (tokens.Length <= tableTokenIndex)
            {
                throw new InvalidOperationException($"Could not parse CREATE TABLE statement: {line}");
            }

            tableNames.Add(tokens[tableTokenIndex].Trim('`', '('));
        }

        return tableNames;
    }

    private static string Sha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class SchemaMigrationRecord
    {
        public string Version { get; set; } = string.Empty;

        public string Checksum { get; set; } = string.Empty;
    }
}
