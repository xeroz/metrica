using Metrica.Contracts;

namespace Processing.Worker.Persistence.Models;

internal sealed class StatusFileRow
{
    public long Id { get; set; }
    public EstadoCarga Status { get; set; }
    public string FileName { get; set; } = "";
    public Guid UniqueIdentifier { get; set; }
}
