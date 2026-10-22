using Control.Api.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Control.Api.Infrastructure.Persistence;

internal sealed class ControlDbInitializer(IDbContextFactory<ControlDbContext> contexts) : IControlDbInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}
