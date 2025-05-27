using Cashflow.SharedKernel.Idempotency;

namespace Cashflow.Operations.Api.Infrastructure.Idempotency;

public class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly HashSet<Guid> _processedKeys = [];

    public Task<bool> ExistsAsync(Guid key)
        => Task.FromResult(_processedKeys.Contains(key));

    public Task RegisterAsync(Guid key)
    {
        _processedKeys.Add(key);
        return Task.CompletedTask;
    }
}
