using Control.Api.Application.Abstractions;
using Control.Api.Bootstrapping;
using Control.Api.Endpoints;
using Control.Api.Exceptions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControlOpenApi();
builder.Services.AddExceptionHandler<UploadExceptionHandler>();
builder.Services.AddProblemDetails();
builder.ConfigureUploadLimits();
builder.Services.AddControlAuthentication(builder.Configuration);
builder.Services.AddControlPersistence(builder.Configuration);
builder.Services.AddControlUploads();

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
