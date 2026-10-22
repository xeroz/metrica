using Processing.Worker.Application.ReadModels;

namespace Processing.Worker.Application.Abstractions;

internal interface INotificationOutbox
{
    Task<IReadOnlyList<PendingNotification>> PendingNotificationsAsync(CancellationToken token);
    Task MarkNotificationSentAsync(long id, CancellationToken token);
}
