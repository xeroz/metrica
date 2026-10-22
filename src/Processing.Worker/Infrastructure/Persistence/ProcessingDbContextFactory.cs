using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Processing.Worker.Persistence;

internal sealed class ProcessingDbContextFactory : IDesignTimeDbContextFactory<ProcessingDbContext>
{
    public ProcessingDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Host=localhost;Database=metrica_app";
        var options = new DbContextOptionsBuilder<ProcessingDbContext>().UseNpgsql(connection).Options;
        return new ProcessingDbContext(options);
    }
}
