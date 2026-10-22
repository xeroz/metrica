using Microsoft.EntityFrameworkCore;
using Processing.Worker.Persistence.Models;

namespace Processing.Worker.Persistence;

internal sealed class ProcessingDbContext(DbContextOptions<ProcessingDbContext> options) : DbContext(options)
{
    internal DbSet<StatusFileRow> StatusFiles => Set<StatusFileRow>();
    internal DbSet<Period> Periods => Set<Period>();
    internal DbSet<ProductCode> Codes => Set<ProductCode>();
    internal DbSet<ProcessedProduct> Products => Set<ProcessedProduct>();
    internal DbSet<ProcessingFailure> Failures => Set<ProcessingFailure>();
    internal DbSet<ProcessingResult> Results => Set<ProcessingResult>();
    internal DbSet<NotificationOutboxMessage> NotificationOutbox => Set<NotificationOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StatusFileRow>(entity =>
        {
            entity.ToTable("StatusFile", table => table.ExcludeFromMigrations());
            entity.HasKey(file => file.Id);
            entity.Property(file => file.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(file => file.Status).HasColumnName("status").HasConversion<short>();
            entity.Property(file => file.FileName).HasColumnName("file_name").IsRequired();
            entity.Property(file => file.UniqueIdentifier).HasColumnName("unique_identifier");
            entity.HasIndex(file => file.UniqueIdentifier).IsUnique();
        });

        modelBuilder.Entity<Period>(entity =>
        {
            entity.ToTable("period");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.Name).IsUnique();
        });

        modelBuilder.Entity<ProductCode>(entity =>
        {
            entity.ToTable("code");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.Name).IsUnique();
        });

        modelBuilder.Entity<ProcessedProduct>(entity =>
        {
            entity.ToTable("product");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(item => item.Price).HasColumnName("price").HasColumnType("numeric(18,2)");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.Property(item => item.CodeId).HasColumnName("code_id");
            entity.Property(item => item.PeriodId).HasColumnName("period_id");
            entity.Property(item => item.LoadId).HasColumnName("load_id");
            entity.Property(item => item.RowNumber).HasColumnName("row_number");
            entity.HasIndex(item => new { item.LoadId, item.RowNumber }).IsUnique();
            entity.HasIndex(item => new { item.PeriodId, item.CodeId, item.Name, item.Price }).IsUnique();
            entity.HasOne(item => item.Code).WithMany().HasForeignKey(item => item.CodeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Period).WithMany().HasForeignKey(item => item.PeriodId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StatusFileRow>().WithMany().HasForeignKey(item => item.LoadId);
        });

        modelBuilder.Entity<ProcessingFailure>(entity =>
        {
            entity.ToTable("processing_failure");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(item => item.LoadId).HasColumnName("load_id");
            entity.Property(item => item.RowNumber).HasColumnName("row_number");
            entity.Property(item => item.Code).HasColumnName("code");
            entity.Property(item => item.Reason).HasColumnName("reason");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => new { item.LoadId, item.RowNumber, item.Reason }).IsUnique();
            entity.HasOne<StatusFileRow>().WithMany().HasForeignKey(item => item.LoadId);
        });

        modelBuilder.Entity<ProcessingResult>(entity =>
        {
            entity.ToTable("processing_result");
            entity.HasKey(item => item.LoadId);
            entity.Property(item => item.LoadId).HasColumnName("load_id");
            entity.Property(item => item.Outcome).HasColumnName("outcome");
            entity.Property(item => item.InsertedCount).HasColumnName("inserted_count");
            entity.Property(item => item.FailedCount).HasColumnName("failed_count");
            entity.Property(item => item.CompletedAt).HasColumnName("completed_at");
            entity.HasOne<StatusFileRow>().WithOne().HasForeignKey<ProcessingResult>(item => item.LoadId);
        });

        modelBuilder.Entity<NotificationOutboxMessage>(entity =>
        {
            entity.ToTable("notification_outbox");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(item => item.LoadId).HasColumnName("load_id");
            entity.Property(item => item.Payload).HasColumnName("payload").HasColumnType("jsonb");
            entity.Property(item => item.SentAt).HasColumnName("sent_at");
            entity.HasIndex(item => item.LoadId).IsUnique();
            entity.HasOne<StatusFileRow>().WithMany().HasForeignKey(item => item.LoadId);
        });
    }
}
