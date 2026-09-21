using Legacy.Api.Contracts;
using Legacy.Application.Abstractions;
using Legacy.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Legacy.Api.Controllers;

[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("deposits")]
    public async Task<ActionResult<PaymentResponse>> Deposit(
        [FromBody] DepositRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new DepositRequestDto
        {
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Provider = request.Provider
        };

        var result = await _paymentService.DepositAsync(dto, cancellationToken);
        return Ok(Map(result));
    }

    [HttpPost("withdrawals")]
    public async Task<ActionResult<PaymentResponse>> Withdraw(
        [FromBody] WithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new WithdrawalRequestDto
        {
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Provider = request.Provider
        };

        var result = await _paymentService.WithdrawAsync(dto, cancellationToken);
        return Ok(Map(result));
    }

    private static PaymentResponse Map(PaymentOperationResultDto result)
    {
        return new PaymentResponse
        {
            CustomerId = result.CustomerId,
            Balance = result.Balance,
            TransactionId = result.TransactionId
        };
    }
}
