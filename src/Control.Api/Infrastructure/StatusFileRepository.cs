using System.Text.Json;
using Control.Api.Application;
using Control.Api.Infrastructure.Persistence;
using Control.Api.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Metrica.Contracts;
using Npgsql;

namespace Control.Api.Infrastructure;

internal sealed class StatusFileRepository(IDbContextFactory<ControlDbContext> contexts) : IStatusFileRepository
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }

    public async Task RegisterAsync(Guid identifier, string fileName, string storagePath, string email,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await contexts.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var file = new StatusFileRow
            {
                Status = EstadoCarga.Pending,
                FileName = fileName,
                Email = email,
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
        catch (Exception ex) when (ex is NpgsqlException or DbUpdateException or IOException)
        {
            throw new UploadUnavailableException("Database is unavailable.", ex);
        }
    }

    public async Task<StatusFile?> GetAsync(Guid identifier, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await contexts.CreateDbContextAsync(cancellationToken);
            return await context.StatusFiles.AsNoTracking()
                .Where(file => file.UniqueIdentifier == identifier)
                .Select(file => new StatusFile(file.Status, file.FileName))
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is NpgsqlException or DbUpdateException or IOException)
        {
            throw new UploadUnavailableException("Database is unavailable.", ex);
        }
    }
}
