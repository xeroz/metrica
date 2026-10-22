using System.Text;
using Control.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace Control.Api.Infrastructure.Messaging;

internal sealed class OutboxPublisher(
    IDbContextFactory<ControlDbContext> contexts,
    IConfiguration configuration,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        do
        {
            try { await PublishPendingAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not publish pending uploads; retrying");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        // ponytail: one publisher instance; add DB leasing if Control scales horizontally.
        var pending = await context.Outbox
            .Where(message => message.SentAt == null)
            .OrderBy(message => message.Id)
            .Take(20)
            .ToListAsync(cancellationToken);
        if (pending.Count == 0) return;

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:User"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };
        await using var rabbit = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await rabbit.CreateChannelAsync(new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true), cancellationToken);
        await channel.QueueDeclareAsync("carga_masiva", durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        foreach (var item in pending)
        {
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = item.Id.ToString()
            };
            await channel.BasicPublishAsync("", "carga_masiva", mandatory: true,
                basicProperties: properties, body: Encoding.UTF8.GetBytes(item.Payload),
                cancellationToken: cancellationToken);
            item.SentAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
