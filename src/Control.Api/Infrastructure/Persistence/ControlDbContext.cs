using Microsoft.EntityFrameworkCore;
using Control.Api.Infrastructure.Persistence.Models;

namespace Control.Api.Infrastructure.Persistence;

internal sealed class ControlDbContext(DbContextOptions<ControlDbContext> options) : DbContext(options)
{
    internal DbSet<StatusFileRow> StatusFiles => Set<StatusFileRow>();
    internal DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StatusFileRow>(entity =>
        {
            entity.ToTable("StatusFile", table =>
                table.HasCheckConstraint("CK_StatusFile_status", "status BETWEEN 1 AND 5"));
            entity.HasKey(file => file.Id);
            entity.Property(file => file.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(file => file.Status).HasColumnName("status").HasConversion<short>();
            entity.Property(file => file.FileName).HasColumnName("file_name").IsRequired();
            entity.Property(file => file.UniqueIdentifier).HasColumnName("unique_identifier");
            entity.HasIndex(file => file.UniqueIdentifier).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("control_outbox");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(message => message.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(message => message.SentAt).HasColumnName("sent_at");
        });
    }
}
