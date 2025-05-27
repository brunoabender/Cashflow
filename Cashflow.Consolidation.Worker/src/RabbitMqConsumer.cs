using System.Text;
using System.Text.Json;
using Cashflow.SharedKernel.Event;
using Dapper;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cashflow.Consolidation.Worker;

public class RabbitMqConsumer(IConnection connection, IConfiguration config) : BackgroundService
{
    private readonly IConnection _connection = connection;
    private readonly string _connectionString = config.GetConnectionString("Postgres")!;
    private IChannel _channel = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync(null, stoppingToken);

        await _channel.QueueDeclareAsync("cashflow.deadletter", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", string.Empty },
            { "x-dead-letter-routing-key", "cashflow.deadletter" }
        };

        
        await _channel.QueueDeclareAsync("cashflow.operations", durable: true, exclusive: false, autoDelete: false, arguments: args!, cancellationToken: stoppingToken);
        await _channel.ExchangeDeclareAsync("cashflow.exchange", ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync("cashflow.operations", "cashflow.exchange", "", cancellationToken: stoppingToken);

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

                var @event = JsonSerializer.Deserialize<TransactionCreatedEvent>(json, options)
                             ?? throw new InvalidOperationException("Evento nulo");

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(stoppingToken);

                using var tx = conn.BeginTransaction();

                var sql = "INSERT INTO transactions (id, amount, type, timestamp, id_potency_key ) VALUES (@Id, @Amount, @Type, @Timestamp, @IdPotencyKey)";

                await conn.ExecuteAsync(sql, @event, tx);
                await tx.CommitAsync(stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);

                Console.WriteLine($"[RabbitMQ] Persistido com sucesso: {@event.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQ] Erro ao processar mensagem: {ex.Message}");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }

            await Task.Yield();
        };

        await _channel.BasicConsumeAsync("cashflow.operations", autoAck: false, consumer, stoppingToken);
    }

}
