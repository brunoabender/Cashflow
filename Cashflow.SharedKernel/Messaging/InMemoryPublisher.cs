using System.Text.Json;
using Cashflow.SharedKernel.Messaging;

namespace Cashflow.Operations.Api.Infrastructure.Messaging;

/// <summary>
/// Implementação simples de IMessagePublisher que apenas imprime os eventos no console.
/// Ideal para testes locais e validação de estrutura sem infraestrutura real.
/// </summary>
public class InMemoryPublisher : IMessagePublisher
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : IDomainEvent
    {
        Console.WriteLine($"[EVENT PUBLISHED] {typeof(T).Name}: {JsonSerializer.Serialize(message)}");
        return Task.CompletedTask;
    }
}
