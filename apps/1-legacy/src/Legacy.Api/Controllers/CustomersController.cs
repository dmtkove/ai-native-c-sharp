using Legacy.Api.Contracts;
using Legacy.Application.Abstractions;
using Legacy.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Legacy.Api.Controllers;

[ApiController]
[Route("customers")]
public class CustomersController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public CustomersController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("{customerId:guid}/balance")]
    public async Task<ActionResult<BalanceResponse>> GetBalance(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetBalanceAsync(customerId, cancellationToken);
        return Ok(new BalanceResponse
        {
            CustomerId = result.CustomerId,
            Balance = result.Balance
        });
    }

    [HttpGet("{customerId:guid}/daily-withdrawal-limit")]
    public async Task<ActionResult<DailyWithdrawalLimitResponse>> GetDailyWithdrawalLimit(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetDailyWithdrawalLimitAsync(customerId, cancellationToken);
        return Ok(Map(result));
    }

    [HttpPut("{customerId:guid}/daily-withdrawal-limit")]
    public async Task<ActionResult<DailyWithdrawalLimitResponse>> SetDailyWithdrawalLimit(
        Guid customerId,
        [FromBody] SetDailyWithdrawalLimitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.SetDailyWithdrawalLimitAsync(
            new SetDailyWithdrawalLimitRequestDto
            {
                CustomerId = customerId,
                Amount = request.Amount
            },
            cancellationToken);

        return Ok(Map(result));
    }

    private static DailyWithdrawalLimitResponse Map(DailyWithdrawalLimitResultDto result)
    {
        return new DailyWithdrawalLimitResponse
        {
            CustomerId = result.CustomerId,
            DailyWithdrawalLimit = result.DailyWithdrawalLimit
        };
    }
}
