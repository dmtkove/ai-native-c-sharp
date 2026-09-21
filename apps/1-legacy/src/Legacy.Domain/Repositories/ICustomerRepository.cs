using Legacy.Domain.Entities;

namespace Legacy.Domain.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Update(Customer customer);
}
