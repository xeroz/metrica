using Gateway.Bootstrapping;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGatewayAuthentication(builder.Configuration);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "gateway" }));
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options => options
        .AddDocument("auth", "Autenticación", "/auth/openapi/v1.json")
        .AddDocument("control", "Cargas", "/control/openapi/v1.json"));
}
app.MapReverseProxy();
app.Run();
