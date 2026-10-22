using System.Text.Json;
using Notifications.Worker.Application;
using Notifications.Worker.Application.Dto;
using RabbitMQ.Client;

namespace Notifications.Worker.Infrastructure.Messaging;

internal sealed class NotificationConsumer(IConfiguration configuration, NotificationService notifications,
    ILogger<NotificationConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ConsumeAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Notification consumer unavailable; reconnecting");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken token)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:User"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };
        await using var rabbit = await factory.CreateConnectionAsync(token);
        await using var channel = await rabbit.CreateChannelAsync(cancellationToken: token);
        await channel.QueueDeclareAsync("notificaciones", durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: token);
        while (!token.IsCancellationRequested)
        {
            var delivery = await channel.BasicGetAsync("notificaciones", autoAck: false, token);
            if (delivery is null)
            {
                await Task.Delay(500, token);
                continue;
            }

            CompletionMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<CompletionMessage>(delivery.Body.Span,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (message is null || message.IdCarga <= 0 || string.IsNullOrWhiteSpace(message.Resultado) ||
                    message.Insertados < 0 || message.Fallidos < 0)
                    throw new JsonException("Invalid completion message.");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Invalid notification message rejected");
                await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, token);
                continue;
            }

            try
            {
                await notifications.NotifyAsync(message, token);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, token);
            }
            catch (InvalidDataException ex)
            {
                logger.LogError(ex, "Notification for load {LoadId} rejected", message.IdCarga);
                await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Notification for load {LoadId} failed; retrying", message.IdCarga);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, token);
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }
    }
}
