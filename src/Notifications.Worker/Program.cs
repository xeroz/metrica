using Microsoft.EntityFrameworkCore;
using Notifications.Worker;
using Notifications.Worker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("ConnectionStrings:Database is required.");
builder.Services.AddDbContextFactory<NotificationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<INotificationStatusRepository, NotificationStatusRepository>();
builder.Services.AddHostedService<NotificationConsumer>();
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "notifications" }));

app.Run();
