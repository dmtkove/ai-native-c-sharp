using Legacy.Application.Dtos;
using MediatR;

namespace Legacy.Application.Queries;

public class GetBalanceQuery : IRequest<BalanceResultDto>
{
    public Guid CustomerId { get; set; }
}
