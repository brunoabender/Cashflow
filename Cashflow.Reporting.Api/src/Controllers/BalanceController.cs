using Cashflow.Reporting.Api.Features.GetBalanceByDate;
using Cashflow.SharedKernel.Balance;
using Microsoft.AspNetCore.Mvc;

namespace Cashflow.Reporting.Api.Controllers;

[ApiController]
[Route("transactions")]
public class TransactionsController(IConfiguration configuration, IRedisBalanceCache cache) : ControllerBase
{
    [HttpGet("balance/{date:datetime}")]
    public async Task<IActionResult> GetBalanceByDate(DateOnly date)
    {
        GetBalanceByDateHandler handler = new(configuration, cache);

        var result = await handler.HandleAsync(date);

        return result.IsSuccess
            ? Ok($"Saldo: {result.Value.Total} Ultima atualização: {result.Value.LastUpdate}")
            : StatusCode(StatusCodes.Status500InternalServerError, result.Errors);
    }
}