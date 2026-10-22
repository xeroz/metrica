using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Metrica.Contracts;
using Processing.Worker;
using Processing.Worker.Persistence;
using Processing.Worker.Persistence.Models;

using var workbook = new MemoryStream();
using (var archive = new ZipArchive(workbook, ZipArchiveMode.Create, leaveOpen: true))
{
    var shared = archive.CreateEntry("xl/sharedStrings.xml");
    using (var writer = new StreamWriter(shared.Open()))
        writer.Write("""
            <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <si><t>Periodo</t></si><si><t>CodigoProducto</t></si>
              <si><t>NombreProducto</t></si><si><t>Precio</t></si>
            </sst>
            """);
    var sheet = archive.CreateEntry("xl/worksheets/sheet1.xml");
    using var sheetWriter = new StreamWriter(sheet.Open());
    sheetWriter.Write("""
        <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>
          <row r="1"><c r="A1" t="s"><v>0</v></c><c r="B1" t="s"><v>1</v></c>
            <c r="C1" t="s"><v>2</v></c><c r="D1" t="s"><v>3</v></c></row>
          <row r="2"><c r="A2" t="inlineStr"><is><t>2025-02</t></is></c>
            <c r="B2" t="inlineStr"><is><t> p001 </t></is></c>
            <c r="D2"><v>12.50</v></c></row>
          <row r="3"/>
          <row r="4"><c r="A4" t="inlineStr"><is><t>bad</t></is></c>
            <c r="B4" t="inlineStr"><is><t>P002</t></is></c></row>
          <row r="5"><c r="A5" t="inlineStr"><is><t>2025-03</t></is></c>
            <c r="B5" t="inlineStr"><is><t>P003</t></is></c>
            <c r="D5"><v>75.51000000000001</v></c></row>
          <row r="6"><c r="A6" t="inlineStr"><is><t>2025-03</t></is></c>
            <c r="B6" t="inlineStr"><is><t>P004</t></is></c>
            <c r="D6"><v>1.234</v></c></row>
        </sheetData></worksheet>
        """);
}
workbook.Position = 0;
var rows = WorkbookReader.Read(workbook);
if (rows.Products.Count != 2 || rows.Failures.Count != 2 ||
    rows.Products[0] != new ProductRow(2, "2025-02", "P001", "Sin nombre", 12.50m) ||
    rows.Products[1].Price != 75.51m || rows.Failures[0].RowNumber != 4 ||
    rows.Failures[1].RowNumber != 6)
    throw new Exception("Workbook validation or cleaning failed.");

if (args.Length > 0)
{
    using var sample = File.OpenRead(args[0]);
    var sampleRows = WorkbookReader.Read(sample);
    if (sampleRows.Products.Count + sampleRows.Failures.Count != 200)
        throw new Exception("The supplied sample did not yield all 200 rows.");
    Console.WriteLine($"Sample: {sampleRows.Products.Count} valid, {sampleRows.Failures.Count} invalid");
}
Console.WriteLine("Processing workbook checks passed.");

var services = new ServiceCollection();
services.AddDbContextFactory<ProcessingDbContext>(options => options
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
using var provider = services.BuildServiceProvider();
var contexts = provider.GetRequiredService<IDbContextFactory<ProcessingDbContext>>();
var repository = new LoadRepository(contexts);
var firstIdentifier = Guid.NewGuid();
var secondIdentifier = Guid.NewGuid();
await using (var context = await contexts.CreateDbContextAsync())
{
    context.StatusFiles.AddRange(
        new StatusFileRow { Id = 1001, UniqueIdentifier = firstIdentifier, FileName = "first.xlsx", Status = EstadoCarga.Pending },
        new StatusFileRow { Id = 1002, UniqueIdentifier = secondIdentifier, FileName = "second.xlsx", Status = EstadoCarga.Pending });
    await context.SaveChangesAsync();
}
if (await repository.StartAsync(1001, firstIdentifier, CancellationToken.None) != EstadoCarga.InProgress)
    throw new Exception("Load did not enter InProgress.");
if (await repository.ReservePeriodsAsync(1001, ["2025-02"], CancellationToken.None) is not null)
    throw new Exception("Available period was rejected.");
await repository.SaveRowsAsync(1001, new WorkbookRows(
    [new ProductRow(2, "2025-02", "P001", "Producto", 12.50m),
     new ProductRow(3, "2025-02", "P001", "Producto", 12.50m)],
    [new RowFailure(4, "P002", "Precio inválido.")]), CancellationToken.None);
await repository.FinishAsync(1001, null, CancellationToken.None);
if (await repository.StartAsync(1002, secondIdentifier, CancellationToken.None) != EstadoCarga.InProgress ||
    await repository.ReservePeriodsAsync(1002, ["2025-02"], CancellationToken.None) is not { } reason ||
    !reason.Contains("ya fue cargado"))
    throw new Exception("Duplicate period was not rejected.");
await repository.RejectAsync(1002, reason, CancellationToken.None);
await using (var context = await contexts.CreateDbContextAsync())
{
    var first = await context.Results.SingleAsync(item => item.LoadId == 1001);
    var second = await context.Results.SingleAsync(item => item.LoadId == 1002);
    if (first.InsertedCount != 1 || first.FailedCount != 2 || second.Outcome != "Rechazado" ||
        await context.NotificationOutbox.CountAsync() != 2 ||
        await context.StatusFiles.CountAsync(item => item.Status == EstadoCarga.Completed) != 2)
        throw new Exception("Persistence, audit, state, or notification outbox failed.");
}
var pending = await repository.PendingNotificationsAsync(CancellationToken.None);
if (pending.Count != 2)
    throw new Exception("Completed loads were not queued for notification.");
await repository.MarkNotificationSentAsync(pending[0].Id, CancellationToken.None);
await repository.MarkNotificationSentAsync(pending[0].Id, CancellationToken.None);
await using (var context = await contexts.CreateDbContextAsync())
{
    var message = await context.NotificationOutbox.SingleAsync(item => item.Id == pending[0].Id);
    var file = await context.StatusFiles.SingleAsync(item => item.Id == message.LoadId);
    if (message.SentAt is null || file.Status != EstadoCarga.Notified ||
        await context.StatusFiles.CountAsync(item => item.Status == EstadoCarga.Completed) != 1)
        throw new Exception("Publishing did not advance only the notified load.");
}
Console.WriteLine("Processing repository checks passed.");
