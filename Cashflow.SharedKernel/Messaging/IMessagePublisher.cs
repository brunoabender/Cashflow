namespace Cashflow.SharedKernel.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : IDomainEvent;
    }
}
