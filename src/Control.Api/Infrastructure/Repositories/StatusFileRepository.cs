using System.Text.Json;
using Control.Api.Application.Abstractions;
using Control.Api.Application.Exceptions;
using Control.Api.Application.ReadModels;
using Control.Api.Infrastructure.Persistence;
using Control.Api.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Metrica.Shared;
using Npgsql;

namespace Control.Api.Infrastructure.Repositories;

internal sealed class StatusFileRepository(IDbContextFactory<ControlDbContext> contexts) : IStatusFileRepository
{
    public async Task RegisterAsync(Guid identifier, string fileName, string storagePath, string email, long userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await contexts.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var file = new StatusFileRow
            {
                Status = LoadStatus.Pending,
                FileName = fileName,
                Email = email,
                UserId = userId,
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

    public async Task<StatusFile?> GetAsync(Guid identifier, long userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await contexts.CreateDbContextAsync(cancellationToken);
            return await context.StatusFiles.AsNoTracking()
                .Where(file => file.UniqueIdentifier == identifier && file.UserId == userId)
                .Select(file => new StatusFile(file.Status, file.FileName))
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is NpgsqlException or DbUpdateException or IOException)
        {
            throw new UploadUnavailableException("Database is unavailable.", ex);
        }
    }
}
