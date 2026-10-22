using Notifications.Worker.Application.ReadModels;

namespace Notifications.Worker.Application.Abstractions;

internal interface INotificationStatusRepository
{
    Task<NotificationStatus?> GetAsync(long loadId, CancellationToken token);
    Task<bool> MarkNotifiedAsync(long loadId, CancellationToken token);
}
