using Legacy.Application.Dtos;
using Legacy.Domain.Exceptions;
using Legacy.Domain.Repositories;
using MediatR;

namespace Legacy.Application.Queries;

public class GetDailyWithdrawalLimitQueryHandler : IRequestHandler<GetDailyWithdrawalLimitQuery, DailyWithdrawalLimitResultDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDailyWithdrawalLimitQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DailyWithdrawalLimitResultDto> Handle(GetDailyWithdrawalLimitQuery request, CancellationToken cancellationToken)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new AccountNotFoundException(request.CustomerId);
        }

        return new DailyWithdrawalLimitResultDto
        {
            CustomerId = customer.Id,
            DailyWithdrawalLimit = customer.DailyWithdrawalLimit
        };
    }
}
