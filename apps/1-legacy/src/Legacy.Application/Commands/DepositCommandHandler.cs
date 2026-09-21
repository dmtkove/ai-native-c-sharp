using Legacy.Application.Dtos;
using Legacy.Domain.Entities;
using Legacy.Domain.Enums;
using Legacy.Domain.Exceptions;
using Legacy.Domain.Providers;
using Legacy.Domain.Repositories;
using MediatR;

namespace Legacy.Application.Commands;

public class DepositCommandHandler : IRequestHandler<DepositCommand, PaymentOperationResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentProviderFactory _paymentProviderFactory;

    public DepositCommandHandler(IUnitOfWork unitOfWork, IPaymentProviderFactory paymentProviderFactory)
    {
        _unitOfWork = unitOfWork;
        _paymentProviderFactory = paymentProviderFactory;
    }

    public async Task<PaymentOperationResultDto> Handle(DepositCommand request, CancellationToken cancellationToken)
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
        var providerResult = await provider.DepositAsync(request.CustomerId, request.Amount, cancellationToken);
        if (!providerResult.IsApproved)
        {
            throw new ProviderDeclinedException(request.Provider, request.Amount);
        }

        customer.Balance += request.Amount;
        _unitOfWork.Customers.Update(customer);

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Type = TransactionType.Deposit,
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
