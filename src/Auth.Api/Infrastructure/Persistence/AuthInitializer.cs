using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Infrastructure.Persistence;

internal sealed class AuthInitializer(AuthDbContext database)
{
    internal Task InitializeAsync() => database.Database.MigrateAsync();
}
