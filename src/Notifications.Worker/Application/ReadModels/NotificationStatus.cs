using Metrica.Shared;

namespace Notifications.Worker.Application.ReadModels;

internal sealed record NotificationStatus(LoadStatus Status, string? Email);
