using NUnit.Framework.Internal;

namespace CheckYourEligibility.LoggingTests
{
    [TestFixture]
    public class ExceptionLoggingMiddlewareTests
    {
        private Mock<RequestDelegate> _nextMock;
        private Mock<TelemetryClient> _telemetryClientMock;
        private ExceptionLoggingMiddleware _middleware;

        [SetUp]
        public void SetUp()
        {
            _nextMock = new Mock<RequestDelegate>(MockBehavior.Strict);
            _telemetryClientMock = new Mock<TelemetryClient>(MockBehavior.Strict);
            _middleware = new ExceptionLoggingMiddleware(_nextMock.Object, _telemetryClientMock.Object);
        }

        [Test]
        public async Task InvokeAsync_WhenExceptionIsThrown_TracksException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var exception = new Exception("Test Exception");
            _nextMock.Setup(n => n.Invoke(context)).ThrowsAsync(exception);

            // Act
            Func<Task> action = async () => await _middleware.InvokeAsync(context);

            // Assert
            await action.Should().ThrowAsync<Exception>();
            _telemetryClientMock.Verify(t => t.TrackException(
                exception,
                It.Is<Dictionary<string, string>>(props => props.ContainsKey("EceApi") && props["EceApi"] == "ExceptionRaised"),
                It.Is<Dictionary<string, double>>(meas => meas.ContainsKey("EceException") && meas["EceException"] == 1.0)
            ));
        }

        [Test]
        public async Task InvokeAsync_WhenNoExceptionIsThrown_DoesNotTrackException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _nextMock.Setup(n => n.Invoke(context)).Returns(Task.CompletedTask);

            // Act
            Func<Task> action = async () => await _middleware.InvokeAsync(context);

            // Assert
            await action.Should().NotThrowAsync();
            _telemetryClientMock.Verify(t => t.TrackException(
                It.IsAny<Exception>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Dictionary<string, double>>()
            ), Times.Never);
        }
    }
}
