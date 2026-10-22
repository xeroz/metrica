namespace Processing.Worker.Persistence.Models;

internal sealed class ProcessingPeriod
{
    public string Period { get; set; } = "";
    public long LoadId { get; set; }
}
