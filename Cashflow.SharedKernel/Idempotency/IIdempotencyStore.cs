namespace Cashflow.SharedKernel.Idempotency
{
    public interface IIdempotencyStore
    {
        Task<bool> ExistsAsync(Guid key);
        Task<bool> TryCreateAsync(Guid key);
    }
}
