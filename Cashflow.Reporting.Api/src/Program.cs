using Cashflow.Reporting.Api.Infrastructure.Balance;
using Cashflow.Reporting.Api.Infrastructure.PostgreeConector;
using Cashflow.Reporting.Api.Infrastructure.PostgresConector;
using Cashflow.SharedKernel.Balance;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Scalar.AspNetCore;
using StackExchange.Redis;
using System.Data;
using System.Text;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var config = builder.Configuration;
        var redisConnection = $"{config["Redis:Host"]}:{config["Redis:Port"]}";

        var jwtSecret = "ChaveSecretaMasNesseCasoNaoÉPorqueEstaNoCodigo";
        var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret));

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey
            };
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("Transacoes", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(ctx =>
                    ctx.User.Claims.Any(c => c.Type == "scope" && (
                        c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("transacoes:read") ||
                        c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("transacoes:write")
                    ))
                );
            });
        });

        // ===== Redis =====
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse($"{redisConnection},abortConnect=false");
            return ConnectionMultiplexer.Connect(options);
        });
        builder.Services.AddScoped<IRedisBalanceCache, RedisBalanceCache>();

        // ===== HealthChecks =====
        builder.Services.AddHealthChecks()
            .AddRedis(redisConnection, name: "redis", failureStatus: HealthStatus.Unhealthy)
            .AddNpgSql(config.GetConnectionString("Postgres")!, "SELECT 1", null, "postgres", HealthStatus.Unhealthy);

        // ===== Postgres =====
        builder.Services.AddScoped<IDbConnection>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new NpgsqlConnection(config.GetConnectionString("Postgres"));
        });
        builder.Services.AddScoped<IPostgresHandler, PostgresHandler>();

        // ===== App/Infra services =====
        builder.Services.Configure<JsonSerializerOptions>(options => { options.PropertyNameCaseInsensitive = true; });

        // ===== Controllers, ModelBinder, Swagger, Scalar =====
        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<FriendlyValidationFilter>();
            options.ModelBinderProviders.Insert(0, new DateOnlyModelBinderProvider());
        });
        builder.Services.AddEndpointsApiExplorer();
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

        var app = builder.Build();

        // ===== Swagger e Scalar =====
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "/openapi/{documentName}.json";
            });
            app.MapScalarApiReference();
        }

        // ===== Middlewares =====
        app.UseAuthentication(); 
        app.UseAuthorization();

        // ===== HealthChecks e Controllers =====
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            AllowCachingResponses = false
        });

        app.MapControllers();

        app.Run();
    }
}
