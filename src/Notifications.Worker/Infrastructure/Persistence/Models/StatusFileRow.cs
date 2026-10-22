using Metrica.Shared;

namespace Notifications.Worker.Infrastructure.Persistence.Models;

internal sealed class StatusFileRow
{
    public long Id { get; set; }
    public LoadStatus Status { get; set; }
    public string? Email { get; set; }
}
