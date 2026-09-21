using Legacy.Application.Dtos;

namespace Legacy.Application.Abstractions;

public interface IPaymentService
{
    Task<PaymentOperationResultDto> DepositAsync(DepositRequestDto request, CancellationToken cancellationToken);

    Task<PaymentOperationResultDto> WithdrawAsync(WithdrawalRequestDto request, CancellationToken cancellationToken);

    Task<BalanceResultDto> GetBalanceAsync(Guid customerId, CancellationToken cancellationToken);

    Task<DailyWithdrawalLimitResultDto> GetDailyWithdrawalLimitAsync(Guid customerId, CancellationToken cancellationToken);

    Task<DailyWithdrawalLimitResultDto> SetDailyWithdrawalLimitAsync(SetDailyWithdrawalLimitRequestDto request, CancellationToken cancellationToken);
}
