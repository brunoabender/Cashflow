using Cashflow.SharedKernel.Messaging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IConnection _connection;

    public RabbitMqPublisher(IConnection connection)
    {
        _connection = connection;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : IDomainEvent
    {

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true,
            outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(10)
);
        await using var channel = await _connection.CreateChannelAsync(channelOptions, cancellationToken);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.ExchangeDeclareAsync("cashflow.exchange", ExchangeType.Fanout, durable: true, cancellationToken: cancellationToken);

        var props = new BasicProperties
        {
            Persistent = true
        };

        await channel.BasicPublishAsync(
          exchange: "cashflow.exchange",
          routingKey: "",
          mandatory: false,
          basicProperties: props,
          body: body,
          cancellationToken: cancellationToken);
    }
}
