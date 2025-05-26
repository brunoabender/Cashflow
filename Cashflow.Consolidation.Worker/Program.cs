using Cashflow.Operations.Api.Infrastructure.Messaging;
using Cashflow.SharedKernel.Json.Converter;
using RabbitMQ.Client;
using System.Text.Json;

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
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.Converters.Add(new UlidJsonConverter());
        });

    })
    .Build();

await host.RunAsync();