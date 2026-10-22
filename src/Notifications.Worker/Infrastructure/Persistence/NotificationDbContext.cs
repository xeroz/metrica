using Microsoft.EntityFrameworkCore;
using Notifications.Worker.Infrastructure.Persistence.Models;

namespace Notifications.Worker.Infrastructure.Persistence;

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

