using Cashflow.Operations.Api.Features.CreateTransaction;
using Cashflow.SharedKernel.Enums;
using Shouldly;


namespace Cashflow.Operations.Api.Tests.Features.CreateTransaction;

public class CreateTransactionRequestTests
{
    [Fact]
    public void Should_Create_Request_With_Correct_Values()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var amount = 250.00m;
        var type = TransactionType.Credit;

        // Act
        var request = new CreateTransactionRequest(idempotencyKey, amount, type);

        // Assert
        request.IdempotencyKey.ShouldBe(idempotencyKey);
        request.Amount.ShouldBe(amount);
        request.Type.ShouldBe(type);
    }

    [Fact]
    public void Should_Be_Immutable()
    {
        // Arrange
        var request1 = new CreateTransactionRequest(Guid.NewGuid(), 100m, TransactionType.Debit);

        // Act
        var request2 = request1 with { Amount = 200m };

        // Assert
        request1.Amount.ShouldBe(100m);
        request2.Amount.ShouldBe(200m);
        request1.IdempotencyKey.ShouldBe(request2.IdempotencyKey);
        request1.Type.ShouldBe(request2.Type);
    }

    [Fact]
    public void Should_Have_Value_Equality()
    {
        // Arrange
        var id = Guid.NewGuid();
        var r1 = new CreateTransactionRequest(id, 100m, TransactionType.Credit);
        var r2 = new CreateTransactionRequest(id, 100m, TransactionType.Credit);

        // Assert
        r1.ShouldBe(r2);
        r1.GetHashCode().ShouldBe(r2.GetHashCode());
    }
}
