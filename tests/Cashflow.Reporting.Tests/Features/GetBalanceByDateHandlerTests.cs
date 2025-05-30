using Cashflow.Reporting.Api.Features.GetBalanceByDate;
using Cashflow.Reporting.Api.Infrastructure.PostgresConector;
using Cashflow.SharedKernel.Balance;
using Cashflow.SharedKernel.Enums;
using Moq;
using Shouldly;

namespace Cashflow.Reporting.Api.Tests.Features.GetBalanceByDate;

public class GetBalanceByDateHandlerTests
{
    private readonly Mock<IPostgresHandler> _mockPostgresHandler = new();
    private readonly Mock<IRedisBalanceCache> _mockCache = new();
    private readonly string _testDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString();

    [Fact]
    public async Task HandleAsync_ShouldReturnCachedResult_WhenCacheExists()
    {
        // Arrange
        var cachedResult = new Dictionary<TransactionType, decimal> {
            { TransactionType.Credit, 100 },
            { TransactionType.Debit, 50 }
        };

        _mockCache.Setup(c => c.GetAsync(_testDate)).ReturnsAsync(cachedResult);

        var handler = new GetBalanceByDateHandler(_mockPostgresHandler.Object, _mockCache.Object);

        // Act
        var result = await handler.HandleAsync(_testDate);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(cachedResult);
        _mockPostgresHandler.Verify(x => x.GetTotalsByType(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnPostgresResult_WhenCacheIsNull()
    {
        // Arrange
        Dictionary<TransactionType, decimal>? cacheResult = null;
        var dbResult = new Dictionary<TransactionType, decimal> {
            { TransactionType.Credit, 200 },
            { TransactionType.Debit, 100 }
        };

        _mockCache.Setup(c => c.GetAsync(_testDate)).ReturnsAsync(cacheResult);
        _mockPostgresHandler.Setup(p => p.GetTotalsByType(_testDate)).ReturnsAsync(dbResult);

        var handler = new GetBalanceByDateHandler(_mockPostgresHandler.Object, _mockCache.Object);

        // Act
        var result = await handler.HandleAsync(_testDate);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(dbResult);
        _mockCache.Verify(c => c.SetAsync(_testDate, dbResult), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenExceptionOccurs()
    {
        // Arrange
        _mockCache.Setup(c => c.GetAsync(_testDate)).ThrowsAsync(new Exception("Redis down"));

        var handler = new GetBalanceByDateHandler(_mockPostgresHandler.Object, _mockCache.Object);

        // Act
        var result = await handler.HandleAsync(_testDate);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Redis down");
    }
}
