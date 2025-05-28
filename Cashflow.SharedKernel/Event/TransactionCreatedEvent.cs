using Cashflow.SharedKernel.Enums;
using Cashflow.SharedKernel.Messaging;

namespace Cashflow.SharedKernel.Event;

public record TransactionCreatedEvent : IDomainEvent
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public TransactionType Type { get; init; }
    public DateTime Timestamp { get; init; }
    public Guid IdPotencyKey { get; init; }

    public TransactionCreatedEvent(Guid id, decimal amount, TransactionType type, DateTime timestamp, Guid idPotencyKey)
    {
        Id = id;
        Amount = amount;
        Type = type;
        Timestamp = timestamp;
        IdPotencyKey = idPotencyKey;
    }
}
