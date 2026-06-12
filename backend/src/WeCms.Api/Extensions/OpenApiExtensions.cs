using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace WeCms.Api.Extensions;

public static class OpenApiExtensions
{
    private const string ExportArg = "--export-openapi";
    private const string OpenApiAssemblyName = "Microsoft.AspNetCore.OpenApi";
    private const string OpenApiDocumentProviderTypeName = "Microsoft.Extensions.ApiDescriptions.OpenApiDocumentProvider";
    private const string DocumentName = "v1";
    private const string StableServerUrl = "http://localhost:5000/";

    public static bool IsExportMode(string[] args)
        => Array.IndexOf(args, ExportArg) >= 0;

    public static string GetExportPath(string[] args)
    {
        var exportArgIndex = Array.IndexOf(args, ExportArg);
        if (exportArgIndex < 0 || exportArgIndex + 1 >= args.Length)
        {
            throw new InvalidOperationException("--export-openapi requires an output path.");
        }

        return args[exportArgIndex + 1];
    }

    public static async Task ExportOpenApiAsync(this WebApplication app, string outputPath)
    {
        app.MapOpenApi();
        var json = await GenerateOpenApiJsonForExportOnlyAsync(app.Services);
        var normalizedJson = NormalizeOpenApiJson(json);

        var dir = Path.GetDirectoryName(outputPath);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(outputPath, normalizedJson, System.Text.Encoding.UTF8);

        Console.WriteLine($"OpenAPI document exported to: {outputPath}");
    }

    // ADR-0008: this reflection is isolated to the explicit --export-openapi CLI path.
    private static async Task<string> GenerateOpenApiJsonForExportOnlyAsync(IServiceProvider services)
    {
        var providerType = Type.GetType($"{OpenApiDocumentProviderTypeName}, {OpenApiAssemblyName}")
            ?? throw new InvalidOperationException($"无法加载类型：{OpenApiDocumentProviderTypeName}");

        var provider = Activator.CreateInstance(providerType, services)
            ?? throw new InvalidOperationException($"无法创建实例：{OpenApiDocumentProviderTypeName}");

        using var writer = new StringWriter();
        var generateMethod = providerType.GetMethod(
            "GenerateAsync",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            [typeof(string), typeof(TextWriter), typeof(OpenApiSpecVersion)],
            null)
            ?? throw new InvalidOperationException("无法定位 OpenAPI GenerateAsync 方法。");

        var task = (Task?)generateMethod.Invoke(provider, [DocumentName, writer, OpenApiSpecVersion.OpenApi3_1])
            ?? throw new InvalidOperationException("OpenAPI GenerateAsync 未返回任务。");

        await task;
        return writer.ToString();
    }

    private static string NormalizeOpenApiJson(string json)
    {
        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidOperationException("OpenAPI document root must be a JSON object.");

        root["servers"] = new JsonArray(
            new JsonObject { ["url"] = StableServerUrl }
        );

        return root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
