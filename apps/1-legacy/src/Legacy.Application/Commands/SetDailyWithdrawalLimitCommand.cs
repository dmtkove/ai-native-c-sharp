using Legacy.Application.Dtos;
using MediatR;

namespace Legacy.Application.Commands;

public class SetDailyWithdrawalLimitCommand : IRequest<DailyWithdrawalLimitResultDto>
{
    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }
}
