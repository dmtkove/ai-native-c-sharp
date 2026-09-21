using Legacy.Application.Dtos;
using Legacy.Domain.Entities;
using Legacy.Domain.Enums;
using Legacy.Domain.Exceptions;
using Legacy.Domain.Providers;
using Legacy.Domain.Repositories;
using MediatR;

namespace Legacy.Application.Commands;

public class WithdrawCommandHandler : IRequestHandler<WithdrawCommand, PaymentOperationResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentProviderFactory _paymentProviderFactory;

    public WithdrawCommandHandler(IUnitOfWork unitOfWork, IPaymentProviderFactory paymentProviderFactory)
    {
        _unitOfWork = unitOfWork;
        _paymentProviderFactory = paymentProviderFactory;
    }

    public async Task<PaymentOperationResultDto> Handle(WithdrawCommand request, CancellationToken cancellationToken)
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

        var provider = _paymentProviderFactory.Create(request.Provider);

        if (customer.Balance < request.Amount)
        {
            throw new InsufficientFundsException(customer.Id, customer.Balance, request.Amount);
        }

        if (customer.DailyWithdrawalLimit is decimal dailyLimit)
        {
            var startOfUtcDay = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);
            var withdrawnToday = await _unitOfWork.Transactions.SumWithdrawalsSinceAsync(
                customer.Id,
                startOfUtcDay,
                cancellationToken);

            if (withdrawnToday + request.Amount > dailyLimit)
            {
                throw new DailyLimitExceededException(customer.Id, dailyLimit, withdrawnToday, request.Amount);
            }
        }

        var providerResult = await provider.WithdrawAsync(request.CustomerId, request.Amount, cancellationToken);
        if (!providerResult.IsApproved)
        {
            throw new ProviderDeclinedException(request.Provider, request.Amount);
        }

        customer.Balance -= request.Amount;
        _unitOfWork.Customers.Update(customer);

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Type = TransactionType.Withdrawal,
            Amount = request.Amount,
            Provider = provider.Name,
            CreatedAt = DateTimeOffset.UtcNow,
            ExternalReference = providerResult.ExternalReference
        };

        await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PaymentOperationResultDto
        {
            CustomerId = customer.Id,
            Balance = customer.Balance,
            TransactionId = transaction.Id
        };
    }
}
