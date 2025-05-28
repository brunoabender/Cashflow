using Cashflow.SharedKernel.Enums;

namespace Cashflow.Reporting.Api.Features.GetBalanceByDate
{
    public record GetBalanceResult(decimal Total, TransactionType TransactionType, DateTime LastUpdate);
}
