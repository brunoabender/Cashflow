using NUlid;

namespace Cashflow.SharedKernel.Idempotency
{
    public interface IIdempotencyStore
    {
        Task<bool> ExistsAsync(Ulid key);
        Task RegisterAsync(Ulid key);
    }
}
