using Cashflow.Reporting.Api.Infrastructure.PostgresConector;
using Cashflow.SharedKernel.Balance;
using Cashflow.SharedKernel.Enums;
using FluentResults;

namespace Cashflow.Reporting.Api.Features.GetBalanceByDate
{
    public class GetBalanceByDateHandler(IPostgresHandler postgresHandler, IRedisBalanceCache cache)
    {
        public async Task<Result<Dictionary<TransactionType, decimal>>> HandleAsync(string date)
        {
            try
            {
                var cached = await cache.GetAsync(date);
                if (cached != null)
                    return Result.Ok(cached);

                var balance = await postgresHandler.GetTotalsByType(date);
                await cache.SetAsync(date, balance);

                return Result.Ok(balance);
            }
            catch (Exception ex) 
            {
                return Result.Fail($"Erro ao tentar obter saldo {ex.Message}");
            }
        }
    }
}
