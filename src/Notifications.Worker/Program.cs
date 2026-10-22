using Notifications.Worker;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<NotificationConsumer>();
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "notifications" }));

app.Run();
