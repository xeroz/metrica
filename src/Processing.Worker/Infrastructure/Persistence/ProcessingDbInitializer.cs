using Microsoft.EntityFrameworkCore;
using Processing.Worker.Application.Abstractions;

namespace Processing.Worker.Persistence;

internal sealed class ProcessingDbInitializer(IDbContextFactory<ProcessingDbContext> contexts)
    : IProcessingDbInitializer
{
    public async Task InitializeAsync(CancellationToken token)
    {
        await using var context = await contexts.CreateDbContextAsync(token);
        await context.Database.MigrateAsync(token);
    }
}
