using Microsoft.EntityFrameworkCore;
using Auth.Api.Infrastructure.Persistence.Models;

namespace Auth.Api.Infrastructure.Persistence;

internal sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    internal DbSet<UserRow> Users => Set<UserRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRow>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Password).HasColumnName("password").IsRequired();
        });
    }
}
