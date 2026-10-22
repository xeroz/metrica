using Processing.Worker;
using Processing.Worker.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContextFactory<ProcessingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException("ConnectionStrings:Database is required.")));
builder.Services.AddSingleton<LoadRepository>();
builder.Services.AddHttpClient<WorkbookStorage>(client =>
    client.BaseAddress = new Uri(builder.Configuration["SeaweedFS:FilerUrl"]
        ?? throw new InvalidOperationException("SeaweedFS:FilerUrl is required.")));
builder.Services.AddHostedService<ProcessingConsumer>();
builder.Services.AddHostedService<NotificationPublisher>();

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "processing" }));
app.Run();
