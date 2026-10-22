using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Metrica.Shared;
using Processing.Worker.Persistence;
using Processing.Worker.Persistence.Models;

namespace Processing.Worker;

internal sealed record PendingNotification(long Id, string Payload);

internal sealed class LoadRepository(IDbContextFactory<ProcessingDbContext> contexts)
{
    internal async Task InitializeAsync(CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        await context.Database.MigrateAsync(token);
    }

    internal async Task<LoadStatus?> StartAsync(long loadId, Guid identifier, CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        var file = await context.StatusFiles.SingleOrDefaultAsync(
            item => item.Id == loadId && item.UniqueIdentifier == identifier, token);
        if (file is null) return null;
        if (file.Status == LoadStatus.Pending)
        {
            file.Status = LoadStatus.InProgress;
            await context.SaveChangesAsync(token);
        }
        return file.Status;
    }

    internal async Task SaveRowsAsync(long loadId, WorkbookRows rows, CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var file = await context.StatusFiles.SingleAsync(item => item.Id == loadId, token);
        if (file.Status != LoadStatus.InProgress)
            throw new InvalidOperationException("Load state changed during processing.");

        var periodNames = rows.Products.Select(item => item.Period).Distinct().ToArray();
        var codeNames = rows.Products.Select(item => item.Code).Distinct().ToArray();
        var periods = await context.Periods.Where(item => periodNames.Contains(item.Name))
            .ToDictionaryAsync(item => item.Name, token);
        var codes = await context.Codes.Where(item => codeNames.Contains(item.Name))
            .ToDictionaryAsync(item => item.Name, token);
        var now = DateTimeOffset.UtcNow;
        foreach (var name in periodNames)
            if (!periods.ContainsKey(name))
            {
                var period = new Period { Name = name, CreatedAt = now, UpdatedAt = now };
                periods.Add(name, period);
                context.Periods.Add(period);
            }
        foreach (var name in codeNames)
            if (!codes.ContainsKey(name))
            {
                var code = new ProductCode { Name = name, CreatedAt = now, UpdatedAt = now };
                codes.Add(name, code);
                context.Codes.Add(code);
            }

        var existingRows = await context.Products.AsNoTracking()
            .Where(item => periodNames.Contains(item.Period.Name) && codeNames.Contains(item.Code.Name))
            .Select(item => new { Period = item.Period.Name, Code = item.Code.Name, item.Name, item.Price })
            .ToListAsync(token);
        var seenRows = existingRows.Select(item => (item.Period, item.Code, item.Name, item.Price)).ToHashSet();
        var existingFailures = await context.Failures.AsNoTracking()
            .Where(item => item.LoadId == loadId)
            .Select(item => new { item.RowNumber, item.Reason }).ToListAsync(token);
        var seenFailures = existingFailures.Select(item => (item.RowNumber, item.Reason)).ToHashSet();
        foreach (var failure in rows.Failures)
            AddFailure(context, seenFailures, loadId, failure);
        foreach (var row in rows.Products)
        {
            if (!seenRows.Add((row.Period, row.Code, row.Name, row.Price)))
            {
                AddFailure(context, seenFailures, loadId,
                    new(row.RowNumber, row.Code, "Duplicado: período, código, nombre y precio ya registrados."));
                continue;
            }
            context.Products.Add(new ProcessedProduct
            {
                Code = codes[row.Code], Period = periods[row.Period],
                Name = row.Name, Price = row.Price, CreatedAt = now, UpdatedAt = now,
                LoadId = loadId, RowNumber = row.RowNumber
            });
        }
        file.Status = LoadStatus.Loaded;
        await context.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    internal async Task RejectAsync(long loadId, string reason, CancellationToken token,
        IReadOnlyList<RowFailure>? rowFailures = null)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        var existing = await context.Failures.AsNoTracking()
            .Where(item => item.LoadId == loadId)
            .Select(item => new { item.RowNumber, item.Reason }).ToListAsync(token);
        var seen = existing.Select(item => (item.RowNumber, item.Reason)).ToHashSet();
        AddFailure(context, seen, loadId, new(0, null, reason));
        if (rowFailures is not null)
            foreach (var failure in rowFailures) AddFailure(context, seen, loadId, failure);
        await context.SaveChangesAsync(token);
        await FinishAsync(loadId, "Rechazado", token);
    }

    internal async Task FinishAsync(long loadId, string? outcome, CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        if (await context.Results.AnyAsync(item => item.LoadId == loadId, token)) return;

        var inserted = await context.Products.CountAsync(item => item.LoadId == loadId, token);
        var failed = await context.Failures.CountAsync(item => item.LoadId == loadId, token);
        outcome ??= failed == 0 ? "Completado" : "CompletadoConErrores";
        var finishedAt = DateTimeOffset.UtcNow;
        var file = await context.StatusFiles.SingleAsync(item => item.Id == loadId, token);
        if (file.Status is not (LoadStatus.InProgress or LoadStatus.Loaded))
            throw new InvalidOperationException("Load state changed before completion.");
        file.Status = LoadStatus.Completed;
        context.Results.Add(new ProcessingResult
        {
            LoadId = loadId, Outcome = outcome, InsertedCount = inserted,
            FailedCount = failed, CompletedAt = finishedAt
        });
        context.NotificationOutbox.Add(new NotificationOutboxMessage
        {
            LoadId = loadId,
            Payload = JsonSerializer.Serialize(new
            {
                idCarga = loadId, fechaFin = finishedAt, resultado = outcome,
                insertados = inserted, fallidos = failed
            })
        });
        await context.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    internal async Task<IReadOnlyList<PendingNotification>> PendingNotificationsAsync(CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        return await context.NotificationOutbox.AsNoTracking()
            .Where(item => item.SentAt == null).OrderBy(item => item.Id).Take(20)
            .Select(item => new PendingNotification(item.Id, item.Payload)).ToListAsync(token);
    }

    internal async Task MarkNotificationSentAsync(long id, CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        var message = await context.NotificationOutbox.SingleOrDefaultAsync(item => item.Id == id, token);
        if (message is null || message.SentAt is not null) return;
        message.SentAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(token);
    }

    private static void AddFailure(ProcessingDbContext context, HashSet<(int RowNumber, string Reason)> seen,
        long loadId, RowFailure failure)
    {
        if (!seen.Add((failure.RowNumber, failure.Reason))) return;
        context.Failures.Add(new ProcessingFailure
        {
            LoadId = loadId, RowNumber = failure.RowNumber, Code = failure.Code,
            Reason = failure.Reason, CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
