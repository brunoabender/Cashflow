using Cashflow.SharedKernel.Balance;
using Cashflow.SharedKernel.Enums;
using StackExchange.Redis;
using System.Text.Json;

namespace Cashflow.Reporting.Api.Infrastructure.Balance;

public class RedisBalanceCache(IConnectionMultiplexer connectionMultiplexer) : IRedisBalanceCache
{
    private readonly IDatabase _db = connectionMultiplexer.GetDatabase();

    public async Task<Dictionary<TransactionType, decimal>?> GetAsync(string date)
    {
        var key = GetKey(date);
        var json = await _db.StringGetAsync(key);

        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<Dictionary<TransactionType, decimal>>(json!);
    }

    public async Task SetAsync(string date, Dictionary<TransactionType, decimal> totals)
    {
        var key = GetKey(date.ToString());
        var json = JsonSerializer.Serialize(totals);
        await _db.StringSetAsync(key, json, TimeSpan.FromMinutes(1));
    }

    private string GetKey(string date) => $"balance:{date:dd-MM-yyyy}";
}
