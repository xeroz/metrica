using Metrica.Shared;

namespace Notifications.Worker;

internal sealed record NotificationStatus(LoadStatus Status, string? Email);

internal interface INotificationStatusRepository
{
    Task<NotificationStatus?> GetAsync(long loadId, CancellationToken token);
    Task<bool> MarkNotifiedAsync(long loadId, CancellationToken token);
}
