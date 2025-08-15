using AccountService.Features.Accounts.Model;
using AccountService.Features.Transactions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AccountService.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
            v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));

            foreach (var property in properties)
                modelBuilder.Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion(dateTimeConverter);
        }

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasConversion(
                    v => v.ToUpperInvariant(),
                    v => v
                );

            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.InterestRate).HasPrecision(5, 2);

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.Property(b => b.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasIndex(e => e.OwnerId)
                .HasDatabaseName("IX_Accounts_OwnerId_Hash")
                .HasMethod("hash");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasConversion(
                    v => v.ToUpperInvariant(),
                    v => v
                );

            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasIndex(e => new { e.AccountId, e.Timestamp })
                .HasDatabaseName("IX_Transactions_AccountId_Timestamp");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_Transactions_Timestamp_GiST")
                .HasMethod("gist");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Account>())
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.ClosedAt ??= DateTime.UtcNow;
            }

        foreach (var entry in ChangeTracker.Entries<Transaction>())
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsCanceled = true;
                entry.Entity.CanceledAt ??= DateTime.UtcNow;
            }

        return await base.SaveChangesAsync(cancellationToken);
    }
}