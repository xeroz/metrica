using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Metrica.Contracts;
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

    internal async Task<EstadoCarga?> StartAsync(long loadId, Guid identifier, CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        var file = await context.StatusFiles.SingleOrDefaultAsync(
            item => item.Id == loadId && item.UniqueIdentifier == identifier, token);
        if (file is null) return null;
        if (file.Status == EstadoCarga.Pending)
        {
            file.Status = EstadoCarga.InProgress;
            await context.SaveChangesAsync(token);
        }
        return file.Status;
    }

    internal async Task<string?> ReservePeriodsAsync(long loadId, IEnumerable<string> periods, CancellationToken token)
    {
        var requested = periods.Distinct().ToArray();
        await using var context = await contexts.CreateDbContextAsync(token);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var reserved = await context.Periods.AsNoTracking()
            .Where(item => requested.Contains(item.Period)).ToListAsync(token);
        var other = reserved.FirstOrDefault(item => item.LoadId != loadId);
        if (other is not null)
        {
            var status = await context.StatusFiles.AsNoTracking()
                .Where(item => item.Id == other.LoadId).Select(item => item.Status).SingleAsync(token);
            return status is EstadoCarga.Pending or EstadoCarga.InProgress
                ? $"Periodo {other.Period} bloqueado por otra carga activa."
                : $"Periodo {other.Period} ya fue cargado.";
        }
        foreach (var period in requested.Except(reserved.Select(item => item.Period)))
            context.Periods.Add(new ProcessingPeriod { Period = period, LoadId = loadId });
        await context.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return null;
    }

    internal async Task SaveRowsAsync(long loadId, WorkbookRows rows, CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var file = await context.StatusFiles.SingleAsync(item => item.Id == loadId, token);
        if (file.Status != EstadoCarga.InProgress)
            throw new InvalidOperationException("Load state changed during processing.");

        var codes = rows.Products.Select(item => item.Code).Distinct().ToArray();
        var existingCodes = await context.Products.AsNoTracking()
            .Where(item => codes.Contains(item.Code))
            .Select(item => item.Code).ToListAsync(token);
        var seenCodes = existingCodes.ToHashSet(StringComparer.Ordinal);
        var existingFailures = await context.Failures.AsNoTracking()
            .Where(item => item.LoadId == loadId)
            .Select(item => new { item.RowNumber, item.Reason }).ToListAsync(token);
        var seenFailures = existingFailures.Select(item => (item.RowNumber, item.Reason)).ToHashSet();
        foreach (var failure in rows.Failures)
            AddFailure(context, seenFailures, loadId, failure);
        foreach (var row in rows.Products)
        {
            if (!seenCodes.Add(row.Code))
            {
                AddFailure(context, seenFailures, loadId,
                    new(row.RowNumber, row.Code, "Existente: CodigoProducto duplicado."));
                continue;
            }
            context.Products.Add(new ProcessedProduct
            {
                Code = row.Code, Name = row.Name, Price = row.Price,
                Period = row.Period, LoadId = loadId, RowNumber = row.RowNumber
            });
        }
        file.Status = EstadoCarga.Loaded;
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
        if (file.Status is not (EstadoCarga.InProgress or EstadoCarga.Loaded))
            throw new InvalidOperationException("Load state changed before completion.");
        file.Status = EstadoCarga.Completed;
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
