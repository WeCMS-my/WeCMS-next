using SqlSugar;
using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class SqlSugarSchemaValidatorTests
{
    [Fact]
    public void SchemaValidator_ImplementsSchemaValidatorAbstraction()
    {
        var validator = new SqlSugarSchemaValidator(
            new CodeFirstModelRegistry([]),
            (_, _) => Task.FromResult(true));

        Assert.IsAssignableFrom<ISqlSugarSchemaValidator>(validator);
    }

    [Fact]
    public async Task SchemaValidator_ReturnsMissingTableResult()
    {
        var validator = new SqlSugarSchemaValidator(
            new CodeFirstModelRegistry([new TestModelProvider(typeof(MissingCodeFirstEntity))]),
            (_, _) => Task.FromResult(false));

        var result = await validator.ValidateAsync(TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        var missingTable = Assert.Single(result.MissingTables);
        Assert.Equal("s3_missing_codefirst", missingTable.TableName);
        Assert.Equal(typeof(MissingCodeFirstEntity), missingTable.ModelType);
    }

    [Fact]
    public async Task SchemaValidator_DetectsMissingColumn()
    {
        var validator = new SqlSugarSchemaValidator(
            new CodeFirstModelRegistry([new TestModelProvider(typeof(SchemaProbeEntity))]),
            (_, _) => Task.FromResult<SqlSugarTableSchema?>(
                new SqlSugarTableSchema(
                    "s10_schema_probe",
                    [new SqlSugarColumnSchema("id", false, null), new SqlSugarColumnSchema("optional_name", true, 64)],
                    [new SqlSugarIndexSchema("ux_s10_schema_probe_code", true)])));

        var result = await validator.ValidateAsync(TestContext.Current.CancellationToken);

        var missingColumn = Assert.Single(result.MissingColumns);
        Assert.Equal("s10_schema_probe", missingColumn.TableName);
        Assert.Equal("code", missingColumn.ColumnName);
    }

    [Fact]
    public async Task SchemaValidator_DetectsNullableMismatch()
    {
        var validator = new SqlSugarSchemaValidator(
            new CodeFirstModelRegistry([new TestModelProvider(typeof(SchemaProbeEntity))]),
            (_, _) => Task.FromResult<SqlSugarTableSchema?>(
                new SqlSugarTableSchema(
                    "s10_schema_probe",
                    CompleteColumns(codeIsNullable: true),
                    [new SqlSugarIndexSchema("ux_s10_schema_probe_code", true)])));

        var result = await validator.ValidateAsync(TestContext.Current.CancellationToken);

        var mismatch = Assert.Single(result.NullableMismatches);
        Assert.Equal("s10_schema_probe", mismatch.TableName);
        Assert.Equal("code", mismatch.ColumnName);
        Assert.False(mismatch.ExpectedNullable);
        Assert.True(mismatch.ActualNullable);
    }

    [Fact]
    public async Task SchemaValidator_DetectsLengthMismatch()
    {
        var validator = new SqlSugarSchemaValidator(
            new CodeFirstModelRegistry([new TestModelProvider(typeof(SchemaProbeEntity))]),
            (_, _) => Task.FromResult<SqlSugarTableSchema?>(
                new SqlSugarTableSchema(
                    "s10_schema_probe",
                    CompleteColumns(codeMaxLength: 16),
                    [new SqlSugarIndexSchema("ux_s10_schema_probe_code", true)])));

        var result = await validator.ValidateAsync(TestContext.Current.CancellationToken);

        var mismatch = Assert.Single(result.LengthMismatches);
        Assert.Equal("s10_schema_probe", mismatch.TableName);
        Assert.Equal("code", mismatch.ColumnName);
        Assert.Equal(32, mismatch.ExpectedMaxLength);
        Assert.Equal(16, mismatch.ActualMaxLength);
    }

    [Fact]
    public async Task SchemaValidator_DetectsIndexMismatch()
    {
        var validator = new SqlSugarSchemaValidator(
            new CodeFirstModelRegistry([new TestModelProvider(typeof(SchemaProbeEntity))]),
            (_, _) => Task.FromResult<SqlSugarTableSchema?>(
                new SqlSugarTableSchema(
                    "s10_schema_probe",
                    CompleteColumns(),
                    [new SqlSugarIndexSchema("ux_s10_schema_probe_code", false)])));

        var result = await validator.ValidateAsync(TestContext.Current.CancellationToken);

        var mismatch = Assert.Single(result.IndexMismatches);
        Assert.Equal("s10_schema_probe", mismatch.TableName);
        Assert.Equal("ux_s10_schema_probe_code", mismatch.IndexName);
        Assert.Equal("unique mismatch", mismatch.Reason);
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

    [SugarTable("s3_missing_codefirst")]
    private sealed class MissingCodeFirstEntity
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }
    }

    [SugarTable("s10_schema_probe")]
    [SugarIndex("ux_s10_schema_probe_code", nameof(Code), OrderByType.Asc, true)]
    private sealed class SchemaProbeEntity
    {
        [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
        public long Id { get; set; }

        [SugarColumn(ColumnName = "code", Length = 32)]
        public string Code { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "optional_name", Length = 64, IsNullable = true)]
        public string? OptionalName { get; set; }
    }

    private static SqlSugarColumnSchema[] CompleteColumns(bool codeIsNullable = false, int codeMaxLength = 32)
    {
        return
        [
            new SqlSugarColumnSchema("id", false, null),
            new SqlSugarColumnSchema("code", codeIsNullable, codeMaxLength),
            new SqlSugarColumnSchema("optional_name", true, 64)
        ];
    }
}
