using Microsoft.EntityFrameworkCore;
using Metrica.Shared;

namespace Notifications.Worker.Infrastructure;

internal sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    internal DbSet<StatusFileRow> StatusFiles => Set<StatusFileRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StatusFileRow>(entity =>
        {
            entity.ToTable("StatusFile", table => table.ExcludeFromMigrations());
            entity.HasKey(file => file.Id);
            entity.Property(file => file.Id).HasColumnName("id");
            entity.Property(file => file.Status).HasColumnName("status").HasConversion<short>();
            entity.Property(file => file.Email).HasColumnName("email");
        });
    }
}

internal sealed class StatusFileRow
{
    public long Id { get; set; }
    public LoadStatus Status { get; set; }
    public string? Email { get; set; }
}
