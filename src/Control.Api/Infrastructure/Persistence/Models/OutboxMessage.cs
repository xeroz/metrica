namespace Control.Api.Infrastructure.Persistence.Models;

internal sealed class OutboxMessage
{
    public long Id { get; set; }
    public string Payload { get; set; } = "";
    public DateTimeOffset? SentAt { get; set; }
}
