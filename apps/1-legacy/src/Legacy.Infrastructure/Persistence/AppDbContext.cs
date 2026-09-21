using Legacy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Legacy.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Name).IsRequired().HasMaxLength(200);
            entity.Property(customer => customer.Balance).HasColumnType("TEXT");
            entity.Property(customer => customer.DailyWithdrawalLimit).HasColumnType("TEXT");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Amount).HasColumnType("TEXT");
            entity.Property(transaction => transaction.Provider).IsRequired().HasMaxLength(50);
            entity.Property(transaction => transaction.ExternalReference).HasMaxLength(200);
            entity.HasOne(transaction => transaction.Customer)
                .WithMany(customer => customer.Transactions)
                .HasForeignKey(transaction => transaction.CustomerId);
        });
    }
}
