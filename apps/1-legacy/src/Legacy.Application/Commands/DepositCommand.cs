using Legacy.Application.Dtos;
using MediatR;

namespace Legacy.Application.Commands;

public class DepositCommand : IRequest<PaymentOperationResultDto>
{
    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }

    public string Provider { get; set; } = string.Empty;
}
