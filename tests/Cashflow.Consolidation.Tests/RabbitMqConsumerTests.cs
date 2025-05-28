using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Cashflow.Consolidation.Worker;
using Cashflow.Consolidation.Worker.Infrastructure.Postgres;
using Cashflow.SharedKernel.Event;
using Moq;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Cashflow.Consolidation.Tests
{
    public class RabbitMqConsumerTests
    {
        [Theory, AutoMoqData]
        public async Task HandleMessageAsync_Should_Persist_And_Ack_When_Success(
            [Frozen] Mock<IPostgresHandler> handlerMock,
            [Frozen] Mock<IChannel> channelMock)
        {
            // Arrange
            var @event = new TransactionCreatedEvent(Guid.NewGuid(), 100, SharedKernel.Enums.TransactionType.Debit, DateTime.UtcNow, Guid.NewGuid());

            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            var sut = new RabbitMqConsumer(handlerMock.Object, Mock.Of<IConnection>());
            typeof(RabbitMqConsumer).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(sut, channelMock.Object);

            // Act
            await sut.HandleMessageAsync(body, 42, CancellationToken.None);

            // Assert
            handlerMock.Verify(h => h.Save(It.Is<TransactionCreatedEvent>(
                e => e.Id == @event.Id && e.Amount == @event.Amount), It.IsAny<CancellationToken>()), Times.Once);

            channelMock.Verify(c => c.BasicAckAsync(42, false, It.IsAny<CancellationToken>()), Times.Once);
            channelMock.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        public async Task HandleMessageAsync_Should_Nack_When_Save_Throws(
            [Frozen] Mock<IPostgresHandler> handlerMock,
            [Frozen] Mock<IChannel> channelMock)
        {
            // Arrange
            var @event = new TransactionCreatedEvent(Guid.NewGuid(), 100, SharedKernel.Enums.TransactionType.Debit, DateTime.UtcNow, Guid.NewGuid());

            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            handlerMock.Setup(h => h.Save(It.IsAny<TransactionCreatedEvent>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));

            var sut = new RabbitMqConsumer(handlerMock.Object, Mock.Of<IConnection>());
            typeof(RabbitMqConsumer).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(sut, channelMock.Object);

            // Act
            await sut.HandleMessageAsync(body, 42, CancellationToken.None);

            // Assert
            handlerMock.Verify(h => h.Save(It.IsAny<TransactionCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            channelMock.Verify(c => c.BasicNackAsync(42, false, false, It.IsAny<CancellationToken>()), Times.Once);
            channelMock.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        public async Task HandleMessageAsync_Should_Nack_When_Json_Is_Invalid(
            [Frozen] Mock<IPostgresHandler> handlerMock,
            [Frozen] Mock<IChannel> channelMock)
        {
            // Arrange
            var invalidJson = "NOT_A_JSON";
            var body = Encoding.UTF8.GetBytes(invalidJson);

            var sut = new RabbitMqConsumer(handlerMock.Object, Mock.Of<IConnection>());
            typeof(RabbitMqConsumer).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(sut, channelMock.Object);

            // Act
            await sut.HandleMessageAsync(body, 42, CancellationToken.None);

            // Assert
            handlerMock.Verify(h => h.Save(It.IsAny<TransactionCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            channelMock.Verify(c => c.BasicNackAsync(42, false, false, It.IsAny<CancellationToken>()), Times.Once);
            channelMock.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        public AutoMoqDataAttribute() : base(() => new Fixture().Customize(new AutoMoqCustomization())) { }
    }
}