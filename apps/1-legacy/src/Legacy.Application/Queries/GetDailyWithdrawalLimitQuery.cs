using Legacy.Application.Dtos;
using MediatR;

namespace Legacy.Application.Queries;

public class GetDailyWithdrawalLimitQuery : IRequest<DailyWithdrawalLimitResultDto>
{
    public Guid CustomerId { get; set; }
}
