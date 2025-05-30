using Cashflow.Reporting.Api.Features.GetBalanceByDate;
using Cashflow.Reporting.Api.Infrastructure.PostgresConector;
using Cashflow.SharedKernel.Balance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cashflow.Reporting.Api.Controllers;

[ApiController]
[Route("transactions")]
public class TransactionsController(IPostgresHandler postgresHandler, IRedisBalanceCache cache) : ControllerBase
{
    [HttpGet("balance")]
    [Authorize(Policy = "Transacoes")]
    public async Task<IActionResult> GetBalanceByDate([FromQuery] string date)
    {
        if (!DateOnly.TryParseExact(date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var  parsedDate))
            return BadRequest("Formato de data inválido. Use dd-MM-yyyy.");

        if (parsedDate > DateOnly.FromDateTime(DateTime.Today))
            return BadRequest("Data futura não é permitida.");

        GetBalanceByDateHandler handler = new(postgresHandler, cache);

        var result = await handler.HandleAsync(date);
        return result.IsSuccess ? Ok(new { date, totals = result.Value }): StatusCode(StatusCodes.Status500InternalServerError, result.Errors);
    }
}