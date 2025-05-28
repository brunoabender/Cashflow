using Cashflow.Reporting.Api.Features.GetBalanceByDate;
using Cashflow.Reporting.Api.Infrastructure.PostgresConector;
using Cashflow.SharedKernel.Balance;
using Microsoft.AspNetCore.Mvc;

namespace Cashflow.Reporting.Api.Controllers;

[ApiController]
[Route("transactions")]
public class TransactionsController(IPostgresHandler postgresHandler, IRedisBalanceCache cache) : ControllerBase
{
    [HttpGet("balance/{date:datetime}")]
    public async Task<IActionResult> GetBalanceByDate(DateOnly date)
    {
        GetBalanceByDateHandler handler = new(postgresHandler, cache);

        var result = await handler.HandleAsync(date);
        return result.IsSuccess
            ? Ok(new { date, totals = result.Value })
            : StatusCode(StatusCodes.Status500InternalServerError, result.Errors);
    }
}