using Cashflow.SharedKernel.Messaging;
using Polly;
using Polly.Wrap;

namespace Cashflow.Operations.Api.Infrastructure.Messaging;

public class ResilientPublisher : IMessagePublisher
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly AsyncPolicyWrap _policy;
    private const int ExceptionsAllowed = 2;
    private const int DurationBreak = 10;
    private const int TimeoutValue = 200;

    public ResilientPublisher(IMessagePublisher inner)
    {
        _messagePublisher = inner;

        var retry = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(TimeoutValue * attempt),
                (ex, ts, retryCount, ctx) =>
                {
                    Console.WriteLine($"[Retry {retryCount}] Falha ao publicar: {ex.Message}");
                });

        var circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(ExceptionsAllowed, 
                durationOfBreak: TimeSpan.FromSeconds(DurationBreak),
                onBreak: (ex, ts) => Console.WriteLine("[CircuitBreaker] Aberto"),
                onReset: () => Console.WriteLine("[CircuitBreaker] Fechado"),
                onHalfOpen: () => Console.WriteLine("[CircuitBreaker] Meio-aberto"));

        _policy = Policy.WrapAsync(retry, circuitBreaker);
    }


    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : IDomainEvent => await _policy.ExecuteAsync(() => _messagePublisher.PublishAsync(message, cancellationToken));
}
