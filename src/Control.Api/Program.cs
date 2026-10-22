using Control.Api.Application;
using Control.Api.Application.Filters;
using Control.Api.Application.Validators;
using Control.Api.Endpoints;
using Control.Api.Exceptions;
using Control.Api.Infrastructure;
using Control.Api.Infrastructure.Messaging;
using Control.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    foreach (var server in document.Servers ?? [])
        server.Url = server.Url?.TrimEnd('/');

    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };
    foreach (var path in document.Paths.Where(path => path.Key.StartsWith("/cargas", StringComparison.Ordinal)))
    {
        if (path.Value.Operations is not { } operations) continue;
        foreach (var operation in operations.Values)
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                }
            ];
    }
    return Task.CompletedTask;
}));
builder.Services.AddExceptionHandler<UploadExceptionHandler>();
builder.Services.AddProblemDetails();
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
builder.Services.AddScoped<UploadValidationFilter>();
builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();
app.UseExceptionHandler();
await app.Services.GetRequiredService<IStatusFileRepository>().InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "control" }));
app.MapUploadEndpoints();
app.Run();
