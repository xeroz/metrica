namespace Processing.Worker.Persistence.Models;

internal sealed class ProcessingResult
{
    public long LoadId { get; set; }
    public string Outcome { get; set; } = "";
    public int InsertedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}
