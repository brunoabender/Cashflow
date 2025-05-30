using RabbitMQ.Client;

namespace Cashflow.Operations.Api.Infrastructure.Messaging
{
    public class RabbitMqConnectionProvider(IConfiguration config) : IHostedService, IDisposable
    {
        private readonly IConfiguration _config = config;
        private IConnection? _connection;

        public IConnection Connection => _connection ?? throw new InvalidOperationException("RabbitMQ connection not initialized");

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["Rabbit:Host"]!,
                Port = int.TryParse(_config["Rabbit:Port"], out var port) ? port : 5672,
                UserName = _config["Rabbit:UserName"]!,
                Password = _config["Rabbit:Password"]!
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _connection?.CloseAsync(cancellationToken)!;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}