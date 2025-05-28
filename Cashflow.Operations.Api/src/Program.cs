using Cashflow.Operations.Api.Features.CreateTransaction;
using Cashflow.Operations.Api.Infrastructure.Idempotency;
using Cashflow.Operations.Api.Infrastructure.Messaging;
using Cashflow.SharedKernel.Idempotency;
using Cashflow.SharedKernel.Messaging;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var config = builder.Configuration;
        var redisConn = config["Redis:ConnectionString"] ?? "localhost:6379";

        var key = Encoding.ASCII.GetBytes("ChaveSecretaMasNesseCasoNaoÉPorqueEstaNoCodigo");

        // 🔐 Autenticação e Autorização
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("Transacoes", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "transacoes:write");
            });
        });

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
            .AddRabbitMQ(sp => sp.GetRequiredService<RabbitMqConnectionProvider>().Connection, name: "rabbitmq", failureStatus: HealthStatus.Unhealthy);

        // Aplicação
        builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
        builder.Services.Decorate<IMessagePublisher, ResilientPublisher>();
        builder.Services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();
        builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionValidator.CreateTransactionRequestValidator>();
        builder.Services.Configure<JsonSerializerOptions>(options => options.PropertyNameCaseInsensitive = true);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // ✅ Swagger + JWT para Scalar
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira: Bearer {seu token JWT}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // ✅ Criação do app
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "/openapi/{documentName}.json";
            });

            app.MapScalarApiReference(); // ativa a UI do Scalar
        }

        app.UseAuthentication(); // ✅ Precisa estar antes do UseAuthorization
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
