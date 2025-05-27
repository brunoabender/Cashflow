using Cashflow.SharedKernel.Balance;
using StackExchange.Redis;
using System.Text.Json;

namespace Cashflow.Reporting.Api.Balance
{
    public class RedisBalanceCache(IConnectionMultiplexer connectionMultiplexer) : IRedisBalanceCache
    {
        private readonly IDatabase _db = connectionMultiplexer.GetDatabase();

        public async Task<decimal?> GetAsync(DateOnly date)
        {
            var key = GetKey(date);
            var json = await _db.StringGetAsync(key);

            if (json.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<decimal>(json!);
        }

        public async Task SetAsync(decimal total)
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var key = GetKey(date);

            var json = JsonSerializer.Serialize(total);
            await _db.StringSetAsync(key, json, TimeSpan.FromMinutes(5));
        }

        private string GetKey(DateOnly date) => $"balance:{date:yyyy-MM-dd}";
    }
}
