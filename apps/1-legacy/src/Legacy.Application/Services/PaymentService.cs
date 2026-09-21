using Legacy.Application.Abstractions;
using Legacy.Application.Commands;
using Legacy.Application.Dtos;
using Legacy.Application.Queries;
using MediatR;

namespace Legacy.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IMediator _mediator;

    public PaymentService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<PaymentOperationResultDto> DepositAsync(DepositRequestDto request, CancellationToken cancellationToken)
    {
        var command = new DepositCommand
        {
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Provider = request.Provider
        };

        return _mediator.Send(command, cancellationToken);
    }

    public Task<PaymentOperationResultDto> WithdrawAsync(WithdrawalRequestDto request, CancellationToken cancellationToken)
    {
        var command = new WithdrawCommand
        {
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Provider = request.Provider
        };

        return _mediator.Send(command, cancellationToken);
    }

    public Task<BalanceResultDto> GetBalanceAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var query = new GetBalanceQuery
        {
            CustomerId = customerId
        };

        return _mediator.Send(query, cancellationToken);
    }

    public Task<DailyWithdrawalLimitResultDto> GetDailyWithdrawalLimitAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var query = new GetDailyWithdrawalLimitQuery
        {
            CustomerId = customerId
        };

        return _mediator.Send(query, cancellationToken);
    }

    public Task<DailyWithdrawalLimitResultDto> SetDailyWithdrawalLimitAsync(SetDailyWithdrawalLimitRequestDto request, CancellationToken cancellationToken)
    {
        var command = new SetDailyWithdrawalLimitCommand
        {
            CustomerId = request.CustomerId,
            Amount = request.Amount
        };

        return _mediator.Send(command, cancellationToken);
    }
}
