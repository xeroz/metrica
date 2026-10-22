using Control.Api.Application;
using Control.Api.Application.Abstractions;
using Control.Api.Application.Filters;
using Control.Api.Application.Validators;
using Control.Api.Endpoints;
using Control.Api.Exceptions;
using Control.Api.Infrastructure.Messaging;
using Control.Api.Infrastructure.Persistence;
using Control.Api.Infrastructure.Repositories;
using Control.Api.Infrastructure.Storage.Providers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
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
var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt["SigningKey"];
if (string.IsNullOrEmpty(key) || Encoding.UTF8.GetByteCount(key) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
if (string.IsNullOrWhiteSpace(jwt["Issuer"]) || string.IsNullOrWhiteSpace(jwt["Audience"]))
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = true,
        ValidIssuer = jwt["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwt["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();
builder.Services.AddDbContextFactory<ControlDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<IUserEmailRepository, UserEmailRepository>();
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
app.UseAuthentication();
app.UseAuthorization();
await app.Services.GetRequiredService<IStatusFileRepository>().InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "control" }));
app.MapUploadEndpoints();
app.Run();
