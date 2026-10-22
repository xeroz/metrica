using Microsoft.EntityFrameworkCore;
using Processing.Worker.Application;
using Processing.Worker.Application.Abstractions;
using Processing.Worker.Infrastructure.Messaging;
using Processing.Worker.Infrastructure.Repositories;
using Processing.Worker.Infrastructure.Storage;
using Processing.Worker.Persistence;

namespace Processing.Worker.Bootstrapping;

internal static class DependencyInjection
{
    internal static IServiceCollection AddProcessingWorker(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database is required.");
        services.AddDbContextFactory<ProcessingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IProcessingDbInitializer, ProcessingDbInitializer>();
        services.AddSingleton<LoadRepository>();
        services.AddSingleton<ILoadRepository>(provider => provider.GetRequiredService<LoadRepository>());
        services.AddSingleton<INotificationOutbox>(provider => provider.GetRequiredService<LoadRepository>());
        services.AddHttpClient<IWorkbookStorage, WorkbookStorage>(client =>
            client.BaseAddress = new Uri(configuration["SeaweedFS:FilerUrl"]
                ?? throw new InvalidOperationException("SeaweedFS:FilerUrl is required.")));
        services.AddSingleton<ProcessingService>();
        services.AddHostedService<ProcessingConsumer>();
        services.AddHostedService<NotificationPublisher>();
        return services;
    }
}
