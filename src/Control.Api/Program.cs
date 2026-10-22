using Control.Api.Application;
using Control.Api.Application.Validators;
using Control.Api.Endpoints;
using Control.Api.Infrastructure;
using Control.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = UploadFileRequestValidator.MaxFileSize + 64 * 1024);
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = UploadFileRequestValidator.MaxFileSize + 64 * 1024);

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("ConnectionStrings:Database is required.");
builder.Services.AddDbContextFactory<ControlDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHttpClient<IFileStorage, SeaweedFileStorage>(client =>
    client.BaseAddress = new Uri(builder.Configuration["SeaweedFS:FilerUrl"]
        ?? throw new InvalidOperationException("SeaweedFS:FilerUrl is required.")));
builder.Services.AddSingleton<IStatusFileRepository, StatusFileRepository>();
builder.Services.AddScoped<UploadService>();
builder.Services.AddScoped<UploadFileRequestValidator>();
builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();
await app.Services.GetRequiredService<IStatusFileRepository>().InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "control" }));
app.MapUploadEndpoints();
app.Run();
