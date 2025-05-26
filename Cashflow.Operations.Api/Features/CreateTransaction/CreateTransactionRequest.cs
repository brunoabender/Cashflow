using Cashflow.SharedKernel.Enums;
using NUlid;

namespace Cashflow.Operations.Api.Features.CreateTransaction
{
    public record CreateTransactionRequest(Ulid IdempotencyKey, decimal Amount, TransactionType Type);
}