using NUlid;

namespace Cashflow.SharedKernel.Messaging
{
    public interface IDomainEvent
    {
        DateTime Timestamp { get; }
        Ulid IdPotencyKey { get; }
    }

}
