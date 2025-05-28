using Cashflow.Operations.Api.Features.CreateTransaction;
using Cashflow.SharedKernel.Enums;
using FluentValidation.TestHelper;
using static Cashflow.Operations.Api.Features.CreateTransaction.CreateTransactionValidator;

namespace Cashflow.Operations.Api.Tests.Features.CreateTransaction;

public class CreateTransactionRequestValidatorTests
{
    private readonly CreateTransactionRequestValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_IdempotencyKey_Is_Empty()
    {
        var request = new CreateTransactionRequest(Guid.Empty, 100, TransactionType.Credit);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
              .WithErrorMessage("IdempotencyKey deve ser um ULID válido e não pode ser zerado.");
    }

    [Fact]
    public void Should_Have_Error_When_Amount_Is_Zero()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), 0, TransactionType.Credit);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Amount)
              .WithErrorMessage("O valor da transação deve ser maior que zero.");
    }

    [Fact]
    public void Should_Have_Error_When_TransactionType_Is_Invalid()
    {
        var invalidType = (TransactionType)999;
        var request = new CreateTransactionRequest(Guid.NewGuid(), 100, invalidType);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Type)
              .WithErrorMessage("Tipo de transação inválido.");
    }

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Request()
    {
        var request = new CreateTransactionRequest(Guid.NewGuid(), 100, TransactionType.Debit);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
