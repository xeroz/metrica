using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Control.Api.Infrastructure.Persistence;

internal sealed class ControlDbContextFactory : IDesignTimeDbContextFactory<ControlDbContext>
{
    public ControlDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Host=localhost;Database=metrica_app";
        var options = new DbContextOptionsBuilder<ControlDbContext>().UseNpgsql(connection).Options;
        return new ControlDbContext(options);
    }
}
