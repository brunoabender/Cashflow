using Cashflow.Consolidation.Worker;
using Cashflow.Consolidation.Worker.Infrastructure.Postgres;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IPostgresHandler, PostgreeHandler>();
builder.Services.AddSingleton<IConnection>(sp =>
{
    var rabbitHost = builder.Configuration["Rabbit:Host"] ?? "rabbitmq";
    var rabbitPort = int.TryParse(builder.Configuration["Rabbit:Port"], out var port) ? port : 5672;

    var factory = new ConnectionFactory
    {
        HostName = rabbitHost,
        Port = rabbitPort,
        UserName = builder.Configuration["Rabbit:UserName"] ?? "guest",
        Password = builder.Configuration["Rabbit:Password"] ?? "guest"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddHostedService<RabbitMqConsumer>();
builder.Services.AddHostedService<RabbitMqDlqReprocessor>();

var host = builder.Build();
await host.RunAsync();