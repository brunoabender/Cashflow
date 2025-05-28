using System.Text;
using System.Text.Json;
using Cashflow.SharedKernel.Event;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Cashflow.Consolidation.Worker.Infrastructure.Postgres;

namespace Cashflow.Consolidation.Worker;


public class RabbitMqConsumer(IPostgresHandler handler, IConnection connection) : BackgroundService
{
    private IChannel _channel = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(null, stoppingToken);

        await _channel.QueueDeclareAsync("cashflow.deadletter", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", string.Empty },
            { "x-dead-letter-routing-key", "cashflow.deadletter" }
        };

        await _channel.QueueDeclareAsync("cashflow.operations", durable: true, exclusive: false, autoDelete: false, arguments: args, cancellationToken: stoppingToken);
        await _channel.ExchangeDeclareAsync("cashflow.exchange", ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync("cashflow.operations", "cashflow.exchange", string.Empty, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            await HandleMessageAsync(ea.Body, ea.DeliveryTag, stoppingToken);
            await Task.Yield();
        };

        await _channel.BasicConsumeAsync("cashflow.operations", autoAck: false, consumer, stoppingToken);
    }

    public async Task HandleMessageAsync(ReadOnlyMemory<byte> body, ulong deliveryTag, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body.Span);

        try
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var @event = JsonSerializer.Deserialize<TransactionCreatedEvent>(json, options)
                         ?? throw new InvalidOperationException("Evento nulo");

            await handler.Save(@event, cancellationToken);

            await _channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: cancellationToken);

            Console.WriteLine($"[RabbitMQ] Persistido com sucesso: {@event.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RabbitMQ] Erro ao processar mensagem: {ex.Message}");
            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
        }
    }
}
