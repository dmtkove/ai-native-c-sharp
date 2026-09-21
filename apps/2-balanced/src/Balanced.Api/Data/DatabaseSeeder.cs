using Microsoft.EntityFrameworkCore;

namespace Balanced.Api.Data;

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
