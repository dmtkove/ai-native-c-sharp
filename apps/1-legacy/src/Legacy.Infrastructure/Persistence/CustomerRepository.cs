using Legacy.Domain.Entities;
using Legacy.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legacy.Infrastructure.Persistence;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers.FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);
    }

    public void Update(Customer customer)
    {
        _dbContext.Customers.Update(customer);
    }
}
