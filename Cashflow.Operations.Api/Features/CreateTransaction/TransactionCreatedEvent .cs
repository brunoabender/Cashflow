using Cashflow.SharedKernel.Enums;
using Cashflow.SharedKernel.Messaging;
using NUlid;

namespace Cashflow.Operations.Api.Features.CreateTransaction;

public record TransactionCreatedEvent : IDomainEvent
{
    public Ulid Id { get; init; }
    public decimal Amount { get; init; }
    public TransactionType Type { get; init; }
    public DateTime Timestamp { get; init; }
    public Ulid IdPotencyKey { get; init; }

    public TransactionCreatedEvent(Ulid id, decimal amount, TransactionType type, DateTime timestamp, Ulid idPotencyKey)
    {
        Id = id;
        Amount = amount;
        Type = type;
        Timestamp = timestamp;
        IdPotencyKey = idPotencyKey;
    }
}
