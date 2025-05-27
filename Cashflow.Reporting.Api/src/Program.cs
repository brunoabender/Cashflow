using Cashflow.Reporting.Api.Balance;
using Cashflow.SharedKernel.Balance;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Scalar.AspNetCore;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var config = builder.Configuration;
        var redisConn = config["Redis:ConnectionString"] ?? "redis:6379";

        // Redis
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse($"{redisConn},abortConnect=false");
            return ConnectionMultiplexer.Connect(options);
        });

        builder.Services.AddScoped<IRedisBalanceCache, RedisBalanceCache>();

        // HealthChecks
        builder.Services.AddHealthChecks()
            .AddRedis(redisConn, name: "redis", failureStatus: HealthStatus.Unhealthy);

        //Postgres
        builder.Services.AddScoped<IDbConnection>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new NpgsqlConnection(config.GetConnectionString("Postgres"));
        });

        

        // Application services
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
