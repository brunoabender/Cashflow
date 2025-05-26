using Cashflow.Operations.Api.Infrastructure.Messaging;
using RabbitMQ.Client;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });
        services.AddHostedService<RabbitMqConsumer>();

    })
    .Build();

await host.RunAsync();