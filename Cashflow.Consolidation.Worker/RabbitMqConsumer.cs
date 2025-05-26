using Cashflow.SharedKernel.Event;
using Cashflow.SharedKernel.Json.Converter;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using NUlid; 

namespace Cashflow.Operations.Api.Infrastructure.Messaging;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private IChannel _channel = null!;

    public RabbitMqConsumer(IConnection connection) => _connection = connection;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync("cashflow.exchange", ExchangeType.Fanout, durable: true);
        var queue = await _channel.QueueDeclareAsync(); 
        await _channel.QueueBindAsync(queue.QueueName, "cashflow.exchange", "");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // ← ignora diferença entre "id" e "Id"
                };

                options.Converters.Add(new UlidJsonConverter());

                var @event = JsonSerializer.Deserialize<TransactionCreatedEvent>(json, options);

                Console.WriteLine($"[RabbitMQ] Evento recebido: {@event?.Id.ToString()} | Valor: {@event?.Amount} | Tipo: {@event?.Type}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQ] Erro ao processar mensagem: {ex.Message}");
            }

            await Task.Yield();
        };

        await _channel.BasicConsumeAsync(queue: queue, autoAck: true, consumer: consumer);
    }

    public override void Dispose()
    {
        _channel.DisposeAsync();
        base.Dispose();
    }
}
