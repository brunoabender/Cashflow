using Cashflow.SharedKernel.Enums;

namespace Cashflow.SharedKernel.Balance
{
    public interface IRedisBalanceCache 
    {
        Task<Dictionary<TransactionType, decimal>?> GetAsync(DateOnly date);
        Task SetAsync(Dictionary<TransactionType, decimal> totals);
    }
}
