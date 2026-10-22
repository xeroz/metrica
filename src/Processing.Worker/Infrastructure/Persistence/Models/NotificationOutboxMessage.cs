namespace Processing.Worker.Persistence.Models;

internal sealed class NotificationOutboxMessage
{
    public long Id { get; set; }
    public long LoadId { get; set; }
    public string Payload { get; set; } = "";
    public DateTimeOffset? SentAt { get; set; }
}
