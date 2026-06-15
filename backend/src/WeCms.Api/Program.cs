using WeCms.Api.Middleware;

var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.MapGet("/", () => Results.Text("WeCMS API"));

app.Run();
