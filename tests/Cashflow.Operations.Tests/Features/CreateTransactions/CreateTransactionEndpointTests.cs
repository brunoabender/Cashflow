using Cashflow.Operations.Api.Features.CreateTransaction;
using Cashflow.SharedKernel.Enums;
using Cashflow.SharedKernel.Event;
using Cashflow.SharedKernel.Idempotency;
using Cashflow.SharedKernel.Messaging;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Cashflow.Operations.Tests.Features.CreateTransactions;

public class CreateTransactionEndpointTests
{
    private readonly Mock<IValidator<CreateTransactionRequest>> _validatorMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly Mock<IIdempotencyStore> _idempotencyMock;
    private readonly Mock<ILogger<CreateTransactionEndpoint>> _loggerMock;
    private readonly CreateTransactionEndpoint _endpoint;

    public CreateTransactionEndpointTests()
    {
        _validatorMock = new Mock<IValidator<CreateTransactionRequest>>();
        _publisherMock = new Mock<IMessagePublisher>();
        _idempotencyMock = new Mock<IIdempotencyStore>();
        _loggerMock = new Mock<ILogger<CreateTransactionEndpoint>>();
        _endpoint = new CreateTransactionEndpoint(_loggerMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_RequestIsInvalid()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), 0, TransactionType.Credit);

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[]
            {
                new FluentValidation.Results.ValidationFailure("Amount", "Amount is required.")
            }));

        var result = await _endpoint.Handle(request, _publisherMock.Object, _idempotencyMock.Object);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message == "Amount is required.");
    }

    [Fact]
    public async Task Should_ReturnFailure_When_RequestIsDuplicated()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), 100, TransactionType.Debit);

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _idempotencyMock
            .Setup(s => s.ExistsAsync(request.IdempotencyKey))
            .ReturnsAsync(true);

        var result = await _endpoint.Handle(request, _publisherMock.Object, _idempotencyMock.Object);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Requisição já processada"));
    }

    [Fact]
    public async Task Should_ReturnFailure_When_IdempotencyFails()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), 100, TransactionType.Credit);

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _idempotencyMock
            .Setup(s => s.ExistsAsync(request.IdempotencyKey))
            .ReturnsAsync(false);

        _idempotencyMock
            .Setup(s => s.TryCreateAsync(request.IdempotencyKey))
            .ReturnsAsync(false);

        var result = await _endpoint.Handle(request, _publisherMock.Object, _idempotencyMock.Object);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("idpotencia"));
    }

    [Fact]
    public async Task Should_ReturnFailure_When_PublishingThrowsException()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), 100, TransactionType.Credit);

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _idempotencyMock
            .Setup(s => s.ExistsAsync(request.IdempotencyKey))
            .ReturnsAsync(false);

        _idempotencyMock
            .Setup(s => s.TryCreateAsync(request.IdempotencyKey))
            .ReturnsAsync(true);

        _publisherMock
            .Setup(p => p.PublishAsync(It.IsAny<TransactionCreatedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro interno"));

        var result = await _endpoint.Handle(request, _publisherMock.Object, _idempotencyMock.Object);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Erro interno"));
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RequestIsValid()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), 100, TransactionType.Debit);

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _idempotencyMock
            .Setup(s => s.ExistsAsync(request.IdempotencyKey))
            .ReturnsAsync(false);

        _idempotencyMock
            .Setup(s => s.TryCreateAsync(request.IdempotencyKey))
            .ReturnsAsync(true);

        _publisherMock
            .Setup(p => p.PublishAsync(It.IsAny<TransactionCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _endpoint.Handle(request, _publisherMock.Object, _idempotencyMock.Object);

        result.IsSuccess.ShouldBeTrue();
    }
}
