using Cashflow.Operations.Api.Features.CreateTransaction;
using Cashflow.Operations.Api.Infrastructure.Idempotency;
using Cashflow.Operations.Api.Infrastructure.Messaging;
using Cashflow.SharedKernel.Idempotency;
using Cashflow.SharedKernel.Messaging;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Scalar.AspNetCore;
using StackExchange.Redis;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var config = builder.Configuration;

        var redisConn = config["Redis:ConnectionString"] ?? "redis:6379";
        var rabbitUser = config["Rabbit:UserName"] ?? "guest";
        var rabbitPass = config["Rabbit:Password"] ?? "guest";
        var rabbitHost = config["Rabbit:Host"] ?? "rabbitmq";

        // Redis
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse($"{redisConn},abortConnect=false");
            return ConnectionMultiplexer.Connect(options);
        });

        // RabbitMQ
        builder.Services.AddSingleton<RabbitMqConnectionProvider>();
        builder.Services.AddSingleton(sp => sp.GetRequiredService<RabbitMqConnectionProvider>().Connection);
        builder.Services.AddHostedService(sp => sp.GetRequiredService<RabbitMqConnectionProvider>());

        // HealthChecks
        builder.Services.AddHealthChecks()
            .AddRedis(redisConn, name: "redis", failureStatus: HealthStatus.Unhealthy)
            .AddRabbitMQ(sp => { var provider = sp.GetRequiredService<RabbitMqConnectionProvider>(); return provider.Connection;}, name: "rabbitmq", failureStatus: HealthStatus.Unhealthy);

        // Application services
        builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
        builder.Services.Decorate<IMessagePublisher, ResilientPublisher>();
        builder.Services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();
        builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionValidator.CreateTransactionRequestValidator>();
        builder.Services.Configure<JsonSerializerOptions>(options => { options.PropertyNameCaseInsensitive = true; });


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
    }
}
