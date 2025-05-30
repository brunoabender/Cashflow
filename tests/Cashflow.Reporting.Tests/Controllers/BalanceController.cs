using Cashflow.Reporting.Api.Controllers;
using Cashflow.Reporting.Api.Features.GetBalanceByDate;
using Cashflow.Reporting.Api.Infrastructure.PostgresConector;
using Cashflow.SharedKernel.Balance;
using Cashflow.SharedKernel.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace Cashflow.Integration.Tests.Controllers
{
    public class BalanceControllerTests
    {
        private readonly Mock<IPostgresHandler> _mockPostgresHandler;
        private readonly Mock<IRedisBalanceCache> _mockCache;
        private readonly TransactionsController _controller;

        public BalanceControllerTests()
        {
            _mockPostgresHandler = new Mock<IPostgresHandler>();
            _mockCache = new Mock<IRedisBalanceCache>();
            _controller = new TransactionsController(_mockPostgresHandler.Object, _mockCache.Object);
        }

        [Fact]
        public async Task GetBalanceByDate_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)).ToString("dd-MM-yyyy");
            var totals = new Dictionary<TransactionType, decimal>
            {
                { TransactionType.Credit, 100 },
                { TransactionType.Debit, 50 }
            };

            var handler = new GetBalanceByDateHandler(_mockPostgresHandler.Object, _mockCache.Object);
            _mockCache.Setup(x => x.GetAsync(date)).ReturnsAsync((Dictionary<TransactionType, decimal>?)null);
            _mockPostgresHandler.Setup(x => x.GetTotalsByType(date)).ReturnsAsync(totals);
            _mockCache.Setup(x => x.SetAsync(date, totals)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.GetBalanceByDate(date);

            // Assert
            var okResult = result.ShouldBeOfType<OkObjectResult>();
            okResult.StatusCode.ShouldBe(200);
            okResult.Value.ShouldNotBeSameAs(totals);
        }

        [Fact]
        public async Task GetBalanceByDate_ReturnsError_WhenHandlerFails()
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)).ToString("dd-MM-yyyy");
            var handler = new GetBalanceByDateHandler(_mockPostgresHandler.Object, _mockCache.Object);

            _mockCache.Setup(x => x.GetAsync(date)).ReturnsAsync((Dictionary<TransactionType, decimal>?)null);
            _mockPostgresHandler.Setup(x => x.GetTotalsByType(date))
                .ThrowsAsync(new Exception("Erro inesperado"));

            // Act
            var result = await _controller.GetBalanceByDate(date);

            // Assert
            result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(500);
        }

        [Fact]
        public async Task GetBalanceByDate_ReturnsError_WhenDataIsNotValid()
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
            var handler = new GetBalanceByDateHandler(_mockPostgresHandler.Object, _mockCache.Object);

            // Act
            var result = await _controller.GetBalanceByDate(date.ToString());

            // Assert
            result.ShouldBeOfType<BadRequestObjectResult>().StatusCode.ShouldBe(400);
        }

        [Fact]
        public async Task GetBalanceByDate_ReturnsError_WhenDataIsFutureDate()
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(3));
            var handler = new GetBalanceByDateHandler(_mockPostgresHandler.Object, _mockCache.Object);

            // Act
            var result = await _controller.GetBalanceByDate(date.ToString());

            // Assert
            result.ShouldBeOfType<BadRequestObjectResult>().StatusCode.ShouldBe(400);
        }
    }
}
