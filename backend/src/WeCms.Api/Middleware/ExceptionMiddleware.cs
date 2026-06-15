using System.Text.Encodings.Web;
using System.Text.Json;
using WeCms.Shared;

namespace WeCms.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException exception)
        {
            await WriteErrorAsync(context, exception.StatusCode, exception.Code, exception.Message);
        }
        catch
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                ApiCodes.SystemError,
                "服务器内部错误");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        int code,
        string message)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("Cannot write error response after the response has started.");
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        await using var writer = new Utf8JsonWriter(
            context.Response.Body,
            new JsonWriterOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

        writer.WriteStartObject();
        writer.WriteNumber("code", code);
        writer.WriteString("msg", message);
        writer.WriteNull("data");
        writer.WriteString("traceId", context.TraceIdentifier);
        writer.WriteEndObject();

        await writer.FlushAsync(context.RequestAborted);
    }
}
