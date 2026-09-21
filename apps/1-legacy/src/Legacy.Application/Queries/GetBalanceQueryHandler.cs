using Legacy.Application.Dtos;
using Legacy.Domain.Exceptions;
using Legacy.Domain.Repositories;
using MediatR;

namespace Legacy.Application.Queries;

public class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, BalanceResultDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBalanceQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BalanceResultDto> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new AccountNotFoundException(request.CustomerId);
        }

        return new BalanceResultDto
        {
            CustomerId = customer.Id,
            Balance = customer.Balance
        };
    }
}
