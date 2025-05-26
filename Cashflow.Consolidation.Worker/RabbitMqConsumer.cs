using Cashflow.SharedKernel.Event;
using Cashflow.SharedKernel.Json.Converter;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Cashflow.Operations.Api.Infrastructure.Messaging;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private IChannel _channel = null!;

    public RabbitMqConsumer(IConnection connection) => _connection = connection;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: "cashflow.deadletter",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", string.Empty },
            { "x-dead-letter-routing-key", "cashflow.deadletter" }
        };

        await _channel.QueueDeclareAsync(
            queue: "cashflow.operations",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync("cashflow.exchange", ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync("cashflow.operations", "cashflow.exchange", string.Empty, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new UlidJsonConverter());

                var @event = JsonSerializer.Deserialize<TransactionCreatedEvent>(json, options);

                Console.WriteLine($"[RabbitMQ] Evento recebido: {@event?.Id} | Valor: {@event?.Amount} | Tipo: {@event?.Type}");

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQ] Erro ao processar mensagem: {ex.Message}");

                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }

            await Task.Yield();
        };

        await _channel.BasicConsumeAsync(
            queue: "cashflow.operations",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    public override void Dispose()
    {
        _channel.DisposeAsync();
        base.Dispose();
    }
}
