using Notifications.Worker.Bootstrapping;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNotificationsWorker(builder.Configuration);

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "notifications" }));
app.Run();
