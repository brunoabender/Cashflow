using Cashflow.SharedKernel.Event;
namespace Cashflow.Consolidation.Worker.Infrastructure.Postgres
{
    public interface IPostgresHandler
    {
        Task Save(TransactionCreatedEvent? @event, CancellationToken cancellationToken);
    }
}
