namespace Processing.Worker.Persistence.Models;

internal sealed class Period
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
