namespace Cashflow.SharedKernel.Idempotency
{
    public interface IIdempotencyStore
    {
        Task<bool> ExistsAsync(Guid key);
        Task RegisterAsync(Guid key);
    }
}
