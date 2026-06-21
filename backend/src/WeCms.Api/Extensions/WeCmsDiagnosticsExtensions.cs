using StackExchange.Profiling;
using WeCms.Data.SqlSugar;

namespace WeCms.Api.Extensions;

public static class WeCmsDiagnosticsExtensions
{
    private const string ConfigurationSection = "Diagnostics:MiniProfiler";

    public static IServiceCollection AddWeCmsDiagnostics(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (!IsMiniProfilerEnabled(environment, configuration))
        {
            return services;
        }

        services.AddMiniProfiler(options =>
        {
            options.RouteBasePath = "/profiler";
            options.EnableServerTimingHeader = true;
        });
        services.AddScoped<ISqlTimingRecorder, MiniProfilerSqlTimingRecorder>();

        return services;
    }

    public static WebApplication UseWeCmsDiagnostics(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (IsMiniProfilerEnabled(app.Environment, app.Configuration))
        {
            app.UseMiniProfiler();
        }

        return app;
    }

    public static bool IsMiniProfilerEnabled(IHostEnvironment environment, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        return environment.IsDevelopment()
            || configuration.GetValue($"{ConfigurationSection}:Enabled", false);
    }
}

public sealed class MiniProfilerSqlTimingRecorder : ISqlTimingRecorder
{
    public void RecordExecuted(SqlTimingRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        Record(record, failed: false);
    }

    public void RecordFailed(SqlTimingRecord record, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(exception);

        Record(record, failed: true);
    }

    private static void Record(SqlTimingRecord record, bool failed)
    {
        var profiler = MiniProfiler.Current;
        if (profiler?.Head is null)
        {
            return;
        }

        var elapsedMs = Math.Max(0, (decimal)record.Elapsed.TotalMilliseconds);
        var startMs = Math.Max(0, profiler.DurationMilliseconds - elapsedMs);
        var timing = new CustomTiming(
            profiler,
            MiniProfilerSqlTimingFormatter.Command(record),
            startMs,
            includeStackTrace: false)
        {
            ExecuteType = failed ? $"{record.OperationType} failed" : record.OperationType,
            DurationMilliseconds = elapsedMs,
            Errored = failed
        };
        profiler.Head.AddCustomTiming("sql", timing);
    }
}

public static class MiniProfilerSqlTimingFormatter
{
    public static string Command(SqlTimingRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.ParametersRedacted.Count == 0)
        {
            return record.SqlTemplate;
        }

        var parameters = string.Join(
            ", ",
            record.ParametersRedacted
                .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                .Select(parameter => $"{parameter.Key}={parameter.Value ?? "null"}"));
        return $"{record.SqlTemplate}\n-- parameters: {parameters}";
    }
}
