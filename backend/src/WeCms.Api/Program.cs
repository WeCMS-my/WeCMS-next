var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Text("WeCMS API"));

app.Run();
