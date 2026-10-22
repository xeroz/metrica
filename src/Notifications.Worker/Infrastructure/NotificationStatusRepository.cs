using Microsoft.EntityFrameworkCore;
using Metrica.Shared;

namespace Notifications.Worker.Infrastructure;

internal sealed class NotificationStatusRepository(IDbContextFactory<NotificationDbContext> contexts)
    : INotificationStatusRepository
{
    public async Task<NotificationStatus?> GetAsync(long loadId, CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        return await context.StatusFiles.AsNoTracking()
            .Where(file => file.Id == loadId)
            .Select(file => new NotificationStatus(file.Status, file.Email))
            .SingleOrDefaultAsync(token);
    }

    public async Task<bool> MarkNotifiedAsync(long loadId, CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        return await context.StatusFiles
            .Where(file => file.Id == loadId && file.Status == LoadStatus.Completed)
            .ExecuteUpdateAsync(setters => setters.SetProperty(file => file.Status, LoadStatus.Notified), token) == 1;
    }
}
