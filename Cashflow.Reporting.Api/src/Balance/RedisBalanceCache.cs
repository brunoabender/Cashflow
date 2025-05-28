using Cashflow.SharedKernel.Balance;
using Cashflow.SharedKernel.Enums;
using StackExchange.Redis;
using System.Text.Json;

namespace Cashflow.Reporting.Api.Balance
{
    public class RedisBalanceCache(IConnectionMultiplexer connectionMultiplexer) : IRedisBalanceCache
    {
        private readonly IDatabase _db = connectionMultiplexer.GetDatabase();

        public async Task<Dictionary<TransactionType, decimal>?> GetAsync(DateOnly date)
        {
            var key = GetKey(date);
            var json = await _db.StringGetAsync(key);

            if (json.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<Dictionary<TransactionType, decimal>>(json!);
        }

        public async Task SetAsync(Dictionary<TransactionType, decimal> totals)
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var key = GetKey(date);

            var json = JsonSerializer.Serialize(totals);
            await _db.StringSetAsync(key, json, TimeSpan.FromMinutes(1));
        }

        private string GetKey(DateOnly date) => $"balance:{date:yyyy-MM-dd}";
    }
}
