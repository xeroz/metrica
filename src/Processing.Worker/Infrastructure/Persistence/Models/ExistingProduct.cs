namespace Processing.Worker.Persistence.Models;

internal sealed class ExistingProduct
{
    public long Id { get; set; }
    public long LoadId { get; set; }
    public int RowNumber { get; set; }
    public string Code { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
