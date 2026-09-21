using Microsoft.EntityFrameworkCore;

namespace AiNative.Api.Data;

public sealed class Customer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public decimal? DailyWithdrawalLimit { get; set; }
}

public sealed class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Provider { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string ExternalReference { get; set; } = string.Empty;
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<PaymentTransaction>().ToTable("Transactions");
    }
}

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Customers.AnyAsync())
        {
            return;
        }

        db.Customers.AddRange(
            new Customer { Id = SeedCustomers.AliceId, Name = "Alice", Balance = 100.00m },
            new Customer { Id = SeedCustomers.BobId, Name = "Bob", Balance = 25.00m });

        await db.SaveChangesAsync();
    }
}
