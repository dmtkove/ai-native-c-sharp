using Legacy.Domain;
using Legacy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Legacy.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext)
    {
        await dbContext.Database.EnsureCreatedAsync();

        if (await dbContext.Customers.AnyAsync())
        {
            return;
        }

        dbContext.Customers.AddRange(
            new Customer { Id = SeedData.AliceId, Name = "Alice", Balance = 100.00m },
            new Customer { Id = SeedData.BobId, Name = "Bob", Balance = 25.00m });

        await dbContext.SaveChangesAsync();
    }
}
