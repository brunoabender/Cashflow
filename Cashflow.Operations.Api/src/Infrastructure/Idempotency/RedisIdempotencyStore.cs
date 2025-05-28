using Cashflow.SharedKernel.Idempotency;
using StackExchange.Redis;

namespace Cashflow.Operations.Api.Infrastructure.Idempotency;

public class RedisIdempotencyStore(IConnectionMultiplexer connectionMultiplexer) : IIdempotencyStore
{
    private readonly IDatabase _redis = connectionMultiplexer.GetDatabase();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(1);

    public async Task<bool> ExistsAsync(Guid key) => await _redis.KeyExistsAsync(GetRedisKey(key));
    public async Task<bool> TryCreateAsync(Guid key) => await _redis.StringSetAsync(GetRedisKey(key), "1", _ttl, When.NotExists);
    
    private static string GetRedisKey(Guid key) => $"idempotency:{key}";
}