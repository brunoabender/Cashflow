using Cashflow.Operations.Api.Features.CreateTransaction;
using Cashflow.Operations.Api.Infrastructure.Idempotency;
using Cashflow.Operations.Api.Infrastructure.Messaging;
using Cashflow.SharedKernel.Idempotency;
using Cashflow.SharedKernel.Json.Converter;
using Cashflow.SharedKernel.Messaging;
using FluentValidation;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IMessagePublisher, InMemoryPublisher>();
builder.Services.Decorate<IMessagePublisher, ResilientPublisher>();
builder.Services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionValidator.CreateTransactionRequestValidator>();

var redisConfig = builder.Configuration.GetSection("Redis:ConnectionString").Value!;
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UlidJsonConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
