using Cashflow.SharedKernel.Enums;

namespace Cashflow.SharedKernel.Balance
{
    public interface IRedisBalanceCache 
    {
        Task<Dictionary<TransactionType, decimal>?> GetAsync(string date);
        Task SetAsync(string date, Dictionary<TransactionType, decimal> totals);
    }
}
