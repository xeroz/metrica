using Metrica.Shared;
using Notifications.Worker.Application.Abstractions;
using Notifications.Worker.Application.Dto;

namespace Notifications.Worker.Application;

internal sealed class NotificationService(INotificationStatusRepository statuses, IEmailSender sender,
    ILogger<NotificationService> logger)
{
    internal async Task NotifyAsync(CompletionMessage message, CancellationToken token)
    {
        var file = await statuses.GetAsync(message.IdCarga, token)
            ?? throw new InvalidDataException($"Load {message.IdCarga} does not exist.");
        if (file.Status == LoadStatus.Notified) return;
        if (file.Status != LoadStatus.Completed)
            throw new InvalidDataException($"Load {message.IdCarga} is not completed.");
        if (string.IsNullOrWhiteSpace(file.Email))
            throw new InvalidDataException($"Load {message.IdCarga} has no recipient email.");

        await sender.SendAsync(message, file.Email, token);

        if (!await statuses.MarkNotifiedAsync(message.IdCarga, token))
            throw new InvalidOperationException($"Load {message.IdCarga} changed state during notification.");
        logger.LogInformation("Email sent for load {LoadId}", message.IdCarga);
    }
}
