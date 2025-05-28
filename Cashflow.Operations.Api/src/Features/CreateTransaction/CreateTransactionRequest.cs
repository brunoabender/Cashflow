using Cashflow.SharedKernel.Enums;

namespace Cashflow.Operations.Api.Features.CreateTransaction
{
    public record CreateTransactionRequest(Guid IdempotencyKey, decimal Amount, TransactionType Type);
}