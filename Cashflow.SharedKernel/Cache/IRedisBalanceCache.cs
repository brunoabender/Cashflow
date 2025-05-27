namespace Cashflow.SharedKernel.Balance
{
    public interface IRedisBalanceCache 
    {
        Task<decimal?> GetAsync(DateOnly date);
        Task SetAsync(decimal total);
    }
}
