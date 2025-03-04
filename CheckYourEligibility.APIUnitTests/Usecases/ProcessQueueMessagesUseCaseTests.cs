using AutoFixture;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class ProcessQueueMessagesUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockService;
        private Mock<ILogger<ProcessQueueMessagesUseCase>> _mockLogger;
        private ProcessQueueMessagesUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<ProcessQueueMessagesUseCase>>(MockBehavior.Loose);
            _sut = new ProcessQueueMessagesUseCase(_mockService.Object, _mockLogger.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockService.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            ICheckEligibility service = null;
            var logger = _mockLogger.Object;

            // Act
            Action act = () => new ProcessQueueMessagesUseCase(service, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'checkService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_logger_is_null()
        {
            // Arrange
            var service = _mockService.Object;
            ILogger<ProcessQueueMessagesUseCase> logger = null;

            // Act
            Action act = () => new ProcessQueueMessagesUseCase(service, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task Execute_returns_invalid_request_message_when_queue_is_null_or_empty(string queueName)
        {
            // Act
            var result = await _sut.Execute(queueName);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().Be("Invalid Request.");
        }

        [Test]
        public async Task Execute_calls_ProcessQueue_on_service_when_queue_name_is_valid()
        {
            // Arrange
            var queueName = _fixture.Create<string>();
            _mockService.Setup(s => s.ProcessQueue(queueName)).Returns(Task.CompletedTask);

            // Act
            await _sut.Execute(queueName);

            // Assert
            _mockService.Verify(s => s.ProcessQueue(queueName), Times.Once);
        }

        [Test]
        public async Task Execute_returns_success_message_when_queue_processing_succeeds()
        {
            // Arrange
            var queueName = _fixture.Create<string>();
            _mockService.Setup(s => s.ProcessQueue(queueName)).Returns(Task.CompletedTask);

            // Act
            var result = await _sut.Execute(queueName);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().Be("Queue Processed.");
        }
    }
}