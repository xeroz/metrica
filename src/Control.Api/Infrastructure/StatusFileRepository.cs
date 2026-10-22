using System.Text.Json;
using Control.Api.Application;
using Control.Api.Infrastructure.Persistence;
using Control.Api.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Metrica.Contracts;

namespace Control.Api.Infrastructure;

internal sealed class StatusFileRepository(IDbContextFactory<ControlDbContext> contexts) : IStatusFileRepository
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }

    public async Task RegisterAsync(Guid identifier, string fileName, string storagePath,
        CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var file = new StatusFileRow
        {
            Status = EstadoCarga.Pending,
            FileName = fileName,
            UniqueIdentifier = identifier
        };
        context.StatusFiles.Add(file);
        await context.SaveChangesAsync(cancellationToken);

        context.Outbox.Add(new OutboxMessage
        {
            Payload = JsonSerializer.Serialize(new
            {
                idCarga = file.Id,
                uniqueIdentifier = identifier,
                rutaArchivo = storagePath
            })
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<StatusFile?> GetAsync(Guid identifier, CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        return await context.StatusFiles.AsNoTracking()
            .Where(file => file.UniqueIdentifier == identifier)
            .Select(file => new StatusFile(file.Status, file.FileName))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
