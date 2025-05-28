using Cashflow.SharedKernel.Event;
using Dapper;

namespace Cashflow.Consolidation.Worker.Infrastructure.Postgres
{
    public class PostgreeHandler(IConfiguration config) : IPostgresHandler
    {
        private readonly string _connectionString = config.GetConnectionString("Postgres")!;

        public async Task Save(TransactionCreatedEvent? @event, CancellationToken cancellationToken)
        {
            var conn = new Npgsql.NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            using var transaction = conn.BeginTransaction();

            const string sql = "INSERT INTO transactions (id, amount, type, timestamp, id_potency_key) VALUES (@Id, @Amount, @Type, @Timestamp, @IdPotencyKey)";

            await conn.ExecuteAsync(sql, @event, transaction);
            transaction.Commit();
        }
    }
}
