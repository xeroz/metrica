namespace Processing.Worker.Persistence.Models;

internal sealed class ProcessedProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long CodeId { get; set; }
    public ProductCode Code { get; set; } = null!;
    public long PeriodId { get; set; }
    public Period Period { get; set; } = null!;
    public long LoadId { get; set; }
    public int RowNumber { get; set; }
}
