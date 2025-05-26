using Cashflow.SharedKernel.Idempotency;
using NUlid;

namespace Cashflow.Operations.Api.Infrastructure.Idempotency;

public class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly HashSet<Ulid> _processedKeys = [];

    public Task<bool> ExistsAsync(Ulid key)
        => Task.FromResult(_processedKeys.Contains(key));

    public Task RegisterAsync(Ulid key)
    {
        _processedKeys.Add(key);
        return Task.CompletedTask;
    }
}
