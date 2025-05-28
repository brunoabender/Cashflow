using Cashflow.Reporting.Api.Infrastructure.PostgresConector;
using Cashflow.SharedKernel.Balance;
using Cashflow.SharedKernel.Enums;
using Dapper;
using FluentResults;
using Npgsql;

namespace Cashflow.Reporting.Api.Features.GetBalanceByDate
{
    public class GetBalanceByDateHandler(IPostgresHandler postgresHandler, IRedisBalanceCache cache)
    {
        public async Task<Result<Dictionary<TransactionType, decimal>>> HandleAsync(DateOnly date)
        {
            var cached = await cache.GetAsync(date);
            if (cached != null)
                return Result.Ok(cached);

            var balance = await postgresHandler.GetTotalsByType(date);
            await cache.SetAsync(balance);

            return Result.Ok(balance);
        }
    }
}
