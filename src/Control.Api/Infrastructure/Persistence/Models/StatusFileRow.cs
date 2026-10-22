using Metrica.Contracts;

namespace Control.Api.Infrastructure.Persistence.Models;

internal sealed class StatusFileRow
{
    public long Id { get; set; }
    public EstadoCarga Status { get; set; }
    public string FileName { get; set; } = "";
    public string? Email { get; set; }
    public long? UserId { get; set; }
    public Guid UniqueIdentifier { get; set; }
}
