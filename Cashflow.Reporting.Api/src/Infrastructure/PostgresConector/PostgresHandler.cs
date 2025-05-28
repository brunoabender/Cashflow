using Cashflow.Reporting.Api.Infrastructure.PostgresConector;
using Cashflow.SharedKernel.Enums;
using Dapper;
using Npgsql;

namespace Cashflow.Reporting.Api.Infrastructure.PostgreeConector
{
    public class PostgresHandler(IConfiguration config) : IPostgresHandler
    {
        private readonly string _connectionString = config.GetConnectionString("Postgres")!;
        public async Task<Dictionary<TransactionType, decimal>> GetTotalsByType(DateOnly date)
        {
            await using var conn = new NpgsqlConnection(_connectionString);

            const string sql = """
                SELECT type, SUM(amount) AS total
                FROM transactions
                WHERE timestamp::date = @Date
                GROUP BY type;
            """;

            var rows = await conn.QueryAsync<(int type, decimal total)>(
                sql,
                new { Date = date.ToDateTime(TimeOnly.MinValue) });

            var result = new Dictionary<TransactionType, decimal>();

            foreach (var row in rows)
            {
                if (Enum.IsDefined(typeof(TransactionType), row.type))
                {
                    result[(TransactionType)row.type] = row.total;
                }
            }

            return result;
        }
    }
}
