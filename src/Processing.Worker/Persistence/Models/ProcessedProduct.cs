namespace Processing.Worker.Persistence.Models;

internal sealed class ProcessedProduct
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Period { get; set; } = "";
    public long LoadId { get; set; }
    public int RowNumber { get; set; }
}
