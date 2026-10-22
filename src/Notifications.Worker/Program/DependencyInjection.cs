using Microsoft.EntityFrameworkCore;
using Notifications.Worker.Application;
using Notifications.Worker.Application.Abstractions;
using Notifications.Worker.Infrastructure.Email;
using Notifications.Worker.Infrastructure.Messaging;
using Notifications.Worker.Infrastructure.Persistence;
using Notifications.Worker.Infrastructure.Repositories;

namespace Notifications.Worker.Bootstrapping;

internal static class DependencyInjection
{
    internal static IServiceCollection AddNotificationsWorker(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database is required.");
        services.AddDbContextFactory<NotificationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<INotificationStatusRepository, NotificationStatusRepository>();
        services.AddSingleton<NotificationEmailFactory>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<NotificationService>();
        services.AddHostedService<NotificationConsumer>();
        return services;
    }
}
