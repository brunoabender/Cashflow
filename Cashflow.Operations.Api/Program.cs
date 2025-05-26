using Cashflow.SharedKernel.Json.Converter;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Scalar.AspNetCore;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);


var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
var rabbitUser = builder.Configuration["Rabbit:UserName"] ?? "guest";
var rabbitPass = builder.Configuration["Rabbit:Password"] ?? "guest";
var rabbitHost = builder.Configuration["Rabbit:Host"] ?? "localhost";
var rabbitConn = $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:5672/";

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConn));
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = rabbitHost,
        UserName = rabbitUser,
        Password = rabbitPass,
        Port = 5672
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddHealthChecks()
    .AddRedis(redisConn, name: "redis", failureStatus: HealthStatus.Unhealthy)
     .AddRabbitMQ(sp =>
     {
         var config = sp.GetRequiredService<IConfiguration>();
         var factory = new ConnectionFactory
         {
             HostName = config["Rabbit:Host"] ?? "localhost",
             Port = int.TryParse(config["Rabbit:Port"], out var port) ? port : 5672,
             UserName = config["Rabbit:UserName"] ?? "guest",
             Password = config["Rabbit:Password"] ?? "guest"
         };
         return factory.CreateConnectionAsync().GetAwaiter().GetResult();
     }, name: "rabbitmq", failureStatus: HealthStatus.Unhealthy);

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNameCaseInsensitive = true;
    options.Converters.Add(new UlidJsonConverter());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    AllowCachingResponses = false
});

app.Run();
