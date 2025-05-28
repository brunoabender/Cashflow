namespace Cashflow.SharedKernel.Messaging
{
    public interface IDomainEvent
    {
        DateTime Timestamp { get; }
        Guid IdPotencyKey { get; }
    }

}
