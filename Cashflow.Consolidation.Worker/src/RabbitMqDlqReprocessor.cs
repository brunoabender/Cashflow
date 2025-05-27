using System.Text;
using System.Text.Json;
using Cashflow.SharedKernel.Event;
using Cashflow.SharedKernel.Json.Converter;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cashflow.Consolidation.Worker;

public class RabbitMqDlqReprocessor(IConnection connection) : BackgroundService
{
    private readonly IConnection _connection = connection!;
    private IChannel _channel = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: "cashflow.deadletter.permanent",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: "cashflow.deadletter",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var retryCount = 0;
            var maxRetries = 3;
            var success = false;
            var props = new BasicProperties();

            while (retryCount < maxRetries && !success)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    options.Converters.Add(new UlidJsonConverter());

                    var @event = JsonSerializer.Deserialize<TransactionCreatedEvent>(json, options);

                    Console.WriteLine($"[DLQ Reprocessador] Tentativa {retryCount + 1}: {@event?.Id} | Valor: {@event?.Amount} | Tipo: {@event?.Type}");

                    await _channel.BasicPublishAsync(
                        exchange: "cashflow.exchange",
                        routingKey: "",
                        mandatory: false,
                        basicProperties: props,
                        body: Encoding.UTF8.GetBytes(json),
                        cancellationToken: stoppingToken);

                    success = true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Console.WriteLine($"[DLQ Reprocessador] Falha tentativa {retryCount}: {ex.Message}");
                }
            }

            if (!success)
            {
                Console.WriteLine("[DLQ Reprocessador] Encaminhando para fila permanente");
                await _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: "cashflow.deadletter.permanent",
                    mandatory: false,
                    basicProperties: props,
                    body: ea.Body,
                    cancellationToken: stoppingToken);
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            await Task.Yield();
        };

        await _channel.BasicConsumeAsync(
            queue: "cashflow.deadletter",
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
