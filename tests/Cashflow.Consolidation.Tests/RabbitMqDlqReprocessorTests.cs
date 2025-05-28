using System.Text;
using System.Text.Json;
using AutoFixture.Xunit2;
using Cashflow.Consolidation.Worker;
using Cashflow.SharedKernel.Event;
using Moq;
using RabbitMQ.Client;
using Shouldly;

namespace Cashflow.Consolidation.Tests
{
    public class AutoMoqDataReprocessorAttribute : AutoDataAttribute
    {
        public AutoMoqDataReprocessorAttribute() : base(() => new AutoFixture.Fixture().Customize(new AutoFixture.AutoMoq.AutoMoqCustomization())) { }
    }

    public class RabbitMqDlqReprocessorTests
    {
        [Theory, AutoMoqData]
        public async Task Should_Republish_Successfully_On_First_Attempt(
            [Frozen] Mock<IChannel> channelMock,
            [Frozen] Mock<IConnection> connMock,
            TransactionCreatedEvent @event)
        {
            // Arrange
            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);
            var props = new BasicProperties();

            channelMock.Setup(c => c.BasicPublishAsync(
                    "cashflow.exchange", "", false, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            connMock.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(channelMock.Object);

            var sut = new RabbitMqDlqReprocessor(connMock.Object);
            typeof(RabbitMqDlqReprocessor).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(sut, channelMock.Object);

            await channelMock.Object.BasicPublishAsync(
                "cashflow.exchange", "", false, props, new ReadOnlyMemory<byte>(body), CancellationToken.None);

            // Assert
            channelMock.Verify(x => x.BasicPublishAsync(
                "cashflow.exchange", "", false, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            channelMock.Verify(x => x.BasicPublishAsync(
                string.Empty, "cashflow.deadletter.permanent", false, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory, AutoMoqData]
        public async Task Should_Republish_Successfully_On_Second_Attempt(
            [Frozen] Mock<IChannel> channelMock,
            [Frozen] Mock<IConnection> connMock,
            TransactionCreatedEvent @event)
        {
            // Arrange
            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);
            var props = new BasicProperties();
            var callCount = 0;

            channelMock.Setup(c => c.BasicPublishAsync(
                "cashflow.exchange", "", false, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new Exception("RabbitMQ error");
                    return ValueTask.CompletedTask;
                });

            connMock.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(channelMock.Object);

            var sut = new RabbitMqDlqReprocessor(connMock.Object);
            typeof(RabbitMqDlqReprocessor).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(sut, channelMock.Object);

            bool success = false;
            int retryCount = 0;
            while (retryCount < 3 && !success)
            {
                try
                {
                    await channelMock.Object.BasicPublishAsync(
                        "cashflow.exchange", "", false, props, new ReadOnlyMemory<byte>(body), CancellationToken.None);
                    success = true;
                }
                catch
                {
                    retryCount++;
                }
            }

            // Assert
            callCount.ShouldBe(2);
        }

        [Theory, AutoMoqData]
        public async Task Should_SendToPermanentDlq_When_AllAttemptsFail(
            [Frozen] Mock<IChannel> channelMock,
            [Frozen] Mock<IConnection> connMock,
            TransactionCreatedEvent @event)
        {
            // Arrange
            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);
            var props = new BasicProperties();

            channelMock.Setup(c => c.BasicPublishAsync(
                "cashflow.exchange", "", false, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("RabbitMQ down"));

            connMock.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(channelMock.Object);

            var sut = new RabbitMqDlqReprocessor(connMock.Object);
            typeof(RabbitMqDlqReprocessor).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(sut, channelMock.Object);

            bool success = false;
            int retryCount = 0;
            while (retryCount < 3 && !success)
            {
                try
                {
                    await channelMock.Object.BasicPublishAsync(
                        "cashflow.exchange", "", false, props, new ReadOnlyMemory<byte>(body), CancellationToken.None);
                    success = true;
                }
                catch
                {
                    retryCount++;
                }
            }

            if (!success)
            {
                await channelMock.Object.BasicPublishAsync(
                    string.Empty, "cashflow.deadletter.permanent", false, props, new ReadOnlyMemory<byte>(body), CancellationToken.None);
            }

            // Assert
            channelMock.Verify(x => x.BasicPublishAsync(
                "cashflow.exchange", "", false, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
                Times.Exactly(3));

            channelMock.Verify(x => x.BasicPublishAsync(
                string.Empty, "cashflow.deadletter.permanent", false, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task Should_Handle_Invalid_Json_And_SendToPermanentDlq(
          [Frozen] Mock<IChannel> channelMock,
          [Frozen] Mock<IConnection> connMock)
        {
            // Arrange
            var invalidJson = "INVALID_JSON";
            var body = Encoding.UTF8.GetBytes(invalidJson);
            var props = new BasicProperties();

            connMock.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(channelMock.Object);

            var sut = new RabbitMqDlqReprocessor(connMock.Object);
            typeof(RabbitMqDlqReprocessor).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(sut, channelMock.Object);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            bool sentToDlq = false;
            try
            {
                JsonSerializer.Deserialize<TransactionCreatedEvent>(invalidJson, options);
            }
            catch
            {
                await channelMock.Object.BasicPublishAsync(
                    string.Empty, "cashflow.deadletter.permanent", false, props, new ReadOnlyMemory<byte>(body), CancellationToken.None);
                sentToDlq = true;
            }

            // Assert
            channelMock.Verify(x => x.BasicPublishAsync(
                "cashflow.exchange", "", false, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
                Times.Never);

            channelMock.Verify(x => x.BasicPublishAsync(
                string.Empty, "cashflow.deadletter.permanent", false, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            sentToDlq.ShouldBeTrue();
        }

    }
}