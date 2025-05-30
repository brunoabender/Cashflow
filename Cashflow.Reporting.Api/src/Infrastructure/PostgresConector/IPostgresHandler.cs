using Cashflow.SharedKernel.Enums;

namespace Cashflow.Reporting.Api.Infrastructure.PostgresConector
{
    public interface IPostgresHandler
    {
        Task<Dictionary<TransactionType, decimal>> GetTotalsByType(string date);
    }
}

