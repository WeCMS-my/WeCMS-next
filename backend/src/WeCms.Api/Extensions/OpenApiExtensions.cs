namespace WeCms.Api.Extensions;

public static class OpenApiExtensions
{
    private const string ExportArg = "--export-openapi";

    public static bool IsExportMode(string[] args)
        => args.Length >= 2 && args[0] == ExportArg;

    public static string GetExportPath(string[] args)
        => args[1];

    public static async Task ExportOpenApiAsync(this WebApplication app, string outputPath)
    {
        app.MapOpenApi();

        // Listen on a random port for the export HTTP request
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();

        try
        {
            // Get the actual bound address
            var addressesFeature = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
                .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            var address = addressesFeature?.Addresses.FirstOrDefault()
                ?? throw new InvalidOperationException("无法获取服务器绑定地址");

            using var client = new HttpClient { BaseAddress = new Uri(address) };
            var json = await client.GetStringAsync("/openapi/v1.json");

            var dir = Path.GetDirectoryName(outputPath);
            if (dir is not null)
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(outputPath, json, System.Text.Encoding.UTF8);

            Console.WriteLine($"OpenAPI document exported to: {outputPath}");
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
