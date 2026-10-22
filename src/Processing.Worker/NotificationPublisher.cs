using System.Text;
using RabbitMQ.Client;

namespace Processing.Worker;

internal sealed class NotificationPublisher(
    LoadRepository repository,
    IConfiguration configuration,
    ILogger<NotificationPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await PublishPendingAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning(ex, "Notification publisher unavailable; retrying"); }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken token)
    {
        var pending = await repository.PendingNotificationsAsync(token);
        if (pending.Count == 0) return;
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:User"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };
        await using var rabbit = await factory.CreateConnectionAsync(token);
        await using var channel = await rabbit.CreateChannelAsync(new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true), token);
        await channel.QueueDeclareAsync("notificaciones", durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: token);
        foreach (var item in pending)
        {
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = item.Id.ToString()
            };
            await channel.BasicPublishAsync("", "notificaciones", mandatory: true,
                basicProperties: properties, body: Encoding.UTF8.GetBytes(item.Payload), cancellationToken: token);
            await repository.MarkNotificationSentAsync(item.Id, token);
        }
    }
}
