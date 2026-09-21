using Legacy.Application.Dtos;
using Legacy.Domain.Exceptions;
using Legacy.Domain.Repositories;
using MediatR;

namespace Legacy.Application.Commands;

public class SetDailyWithdrawalLimitCommandHandler : IRequestHandler<SetDailyWithdrawalLimitCommand, DailyWithdrawalLimitResultDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public SetDailyWithdrawalLimitCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DailyWithdrawalLimitResultDto> Handle(SetDailyWithdrawalLimitCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidAmountException(request.Amount);
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new AccountNotFoundException(request.CustomerId);
        }

        customer.DailyWithdrawalLimit = request.Amount;
        _unitOfWork.Customers.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DailyWithdrawalLimitResultDto
        {
            CustomerId = customer.Id,
            DailyWithdrawalLimit = customer.DailyWithdrawalLimit
        };
    }
}
