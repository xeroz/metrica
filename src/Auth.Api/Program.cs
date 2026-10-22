using Auth.Api.Application;
using Auth.Api.Application.Validators;
using Auth.Api.Endpoints;
using Auth.Api.Infrastructure;
using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    foreach (var server in document.Servers ?? [])
        server.Url = server.Url?.TrimEnd('/');
    return Task.CompletedTask;
}));
var key = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrEmpty(key) || System.Text.Encoding.UTF8.GetByteCount(key) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"]) ||
    string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]))
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");

builder.Services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("ConnectionStrings:Database is required.")));
builder.Services.AddScoped<IPasswordHasher<UserRow>, PasswordHasher<UserRow>>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<ITokenIssuer, JwtIssuer>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<CreateUserService>();
builder.Services.AddScoped<RegisterRequestValidator>();
builder.Services.AddScoped<AuthInitializer>();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<AuthInitializer>().InitializeAsync();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "auth" }));
app.MapAuthEndpoints();

app.Run();
