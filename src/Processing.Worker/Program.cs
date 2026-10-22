using Processing.Worker.Bootstrapping;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProcessingWorker(builder.Configuration);

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "processing" }));
app.Run();
