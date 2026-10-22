using Metrica.Shared;
using Processing.Worker.Application.Abstractions;

namespace Processing.Worker.Application;

internal sealed class ProcessingService(
    ILoadRepository repository,
    IWorkbookStorage storage,
    ILogger<ProcessingService> logger)
{
    internal async Task ProcessAsync(long loadId, Guid identifier, CancellationToken token)
    {
        var status = await repository.StartAsync(loadId, identifier, token);
        if (status is null)
        {
            logger.LogWarning("Ignoring load {LoadId}: no matching StatusFile", loadId);
            return;
        }
        if (status is LoadStatus.Completed or LoadStatus.Notified) return;
        if (status == LoadStatus.Loaded)
        {
            await repository.FinishAsync(loadId, null, token);
            return;
        }

        WorkbookRows rows;
        try
        {
            await using var workbook = await storage.OpenAsync(identifier, token);
            rows = WorkbookReader.Read(workbook);
        }
        catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException
                                   or InvalidOperationException or FormatException)
        {
            await repository.RejectAsync(loadId, $"Excel inválido: {ex.Message}", token);
            return;
        }

        if (rows.Products.Count == 0)
        {
            await repository.RejectAsync(loadId, "El Excel no contiene productos válidos.",
                token, rows.Failures);
            return;
        }
        await repository.SaveRowsAsync(loadId, rows, token);
        await repository.FinishAsync(loadId, null, token);
        logger.LogInformation("Load {LoadId} finished", loadId);
    }
}
