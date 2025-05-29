using System.Text;
using System.Text.Json;
using Cashflow.SharedKernel.Event;
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
            var props = ea.BasicProperties;
            int retryCount = 0;
            int maxRetries = 3;

            if (props?.Headers != null && props.Headers.TryGetValue("x-retry", out var headerVal))
            {
                if (headerVal is byte[] bytes)
                    retryCount = int.Parse(Encoding.UTF8.GetString(bytes));
                else
                    retryCount = Convert.ToInt32(headerVal);
            }

            retryCount++;
            bool success = false;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var @event = JsonSerializer.Deserialize<TransactionCreatedEvent>(json, options);

                Console.WriteLine($"[DLQ Reprocessador] Tentativa {retryCount}: {@event?.Id} | Valor: {@event?.Amount} | Tipo: {@event?.Type}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DLQ Reprocessador] Falha tentativa {retryCount}: {ex.Message}");
                success = false;
            }

            if (!success && retryCount < maxRetries)
            {
                // Cria novos headers propagando e incrementando x-retry
                var retryProps = new BasicProperties();
                retryProps.Headers = props?.Headers != null
                    ? new Dictionary<string, object>(props.Headers)
                    : new Dictionary<string, object>();
                retryProps.Headers["x-retry"] = Encoding.UTF8.GetBytes(retryCount.ToString());

                Console.WriteLine($"[DLQ Reprocessador] Reenfileirando para DLQ tentativa {retryCount}");

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "cashflow.deadletter",
                    mandatory: false,
                    basicProperties: retryProps,
                    body: ea.Body,
                    cancellationToken: stoppingToken);
            }
            else if (!success)
            {
                Console.WriteLine("[DLQ Reprocessador] Encaminhando para fila permanente");

                var permProps = new BasicProperties();
                permProps.Headers = props?.Headers != null
                    ? new Dictionary<string, object>(props.Headers)
                    : new Dictionary<string, object>();

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "cashflow.deadletter.permanent",
                    mandatory: false,
                    basicProperties: permProps,
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
}
