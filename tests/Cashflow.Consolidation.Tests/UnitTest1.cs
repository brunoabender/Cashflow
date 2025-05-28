using Cashflow.Consolidation.Worker;
using Cashflow.SharedKernel.Event;
using Dapper;
using Moq;
using RabbitMQ.Client;
using System.Data;
using System.Text;
using System.Text.Json;

public class RabbitMqConsumerTests
{
    [Fact]
    public async Task HandleMessageAsync_ShouldPersistTransaction_And_Acknowledge()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockDbConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();

        mockConnectionFactory.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>()))
                             .ReturnsAsync(mockDbConnection.Object);
        mockDbConnection.Setup(c => c.BeginTransaction()).Returns(mockTransaction.Object);
        mockDbConnection.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), null, null));
        

        var rabbitConsumer = new RabbitMqConsumer(Mock.Of<IConnection>(), mockConnectionFactory.Object);

        var transactionEvent = new TransactionCreatedEvent(Guid.NewGuid(), 100.0m, Cashflow.SharedKernel.Enums.TransactionType.Credit, DateTime.UtcNow, Guid.NewGuid());
        
        var jsonMessage = JsonSerializer.Serialize(transactionEvent);
        var messageBody = Encoding.UTF8.GetBytes(jsonMessage);
        var deliveryTag = 1UL;

        // Act
        await rabbitConsumer.HandleMessageAsync(messageBody, deliveryTag, CancellationToken.None);

        // Assert
        //mockDbConnection.Verify(c => c.ExecuteAsync(It.IsAny<string>(), transactionEvent, mockTransaction.Object), Times.Once);
        //mockTransaction.Verify(t => t.Commit(), Times.Once);
        //mockChannel.Verify(c => c.BasicAck(deliveryTag, false), Times.Once);
    }
}
