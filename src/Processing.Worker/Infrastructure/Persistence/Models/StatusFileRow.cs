using Metrica.Shared;

namespace Processing.Worker.Persistence.Models;

internal sealed class StatusFileRow
{
    public long Id { get; set; }
    public LoadStatus Status { get; set; }
    public string FileName { get; set; } = "";
    public Guid UniqueIdentifier { get; set; }
}
