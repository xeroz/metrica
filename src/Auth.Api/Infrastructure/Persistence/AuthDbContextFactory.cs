using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Auth.Api.Infrastructure.Persistence;

internal sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Host=localhost;Database=metrica_app";
        var options = new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(connection).Options;
        return new AuthDbContext(options);
    }
}
