using Cashflow.SharedKernel.Balance;
using Cashflow.SharedKernel.Enums;
using Dapper;
using FluentResults;
using Npgsql;

namespace Cashflow.Reporting.Api.Features.GetBalanceByDate
{
    public class GetBalanceByDateHandler(IConfiguration config, IRedisBalanceCache cache)
    {
        private readonly string _connectionString = config.GetConnectionString("Postgres")!;

        public async Task<Result<GetBalanceResult>> HandleAsync(DateOnly date)
        {
            var cached = await cache.GetAsync(date);
            if (cached.HasValue)
                return Result.Ok(new GetBalanceResult(cached.Value, TransactionType.All, DateTime.UtcNow));

            await using var conn = new NpgsqlConnection(_connectionString);
            const string sql = "SELECT COALESCE(SUM(amount), 0) FROM transactions WHERE timestamp::date = @Date";

            var balance = await conn.QuerySingleAsync<decimal>(sql, new { Date = date.ToDateTime(TimeOnly.MinValue) });

            await cache.SetAsync(balance);

            return Result.Ok(new GetBalanceResult(balance, TransactionType.All, DateTime.UtcNow));
        }


    }
}
