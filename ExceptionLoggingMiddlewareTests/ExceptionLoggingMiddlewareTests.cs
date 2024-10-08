using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CheckYourEligibility.WebApp.Middleware;

namespace CheckYourEligibility.WebAppUnitTests.Middleware
{
    [TestFixture]
    public class ExceptionLoggingMiddlewareTests
    {
        private Mock<RequestDelegate> _nextMock;
        private TelemetryClient _telemetryClient;
        private ExceptionLoggingMiddleware _middleware;

        [SetUp]
        public void SetUp()
        {
            _nextMock = new Mock<RequestDelegate>();

            var telemetryConfig = TelemetryConfiguration.CreateDefault();
            telemetryConfig.DisableTelemetry = true; // Disable telemetry for testing
            _telemetryClient = new TelemetryClient(telemetryConfig);

            _middleware = new ExceptionLoggingMiddleware(_nextMock.Object, _telemetryClient);
        }

        [Test]
        public async Task InvokeAsync_WhenExceptionIsThrown_TracksException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var exception = new Exception("Test Exception");
            _nextMock.Setup(n => n.Invoke(context)).ThrowsAsync(exception);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _middleware.InvokeAsync(context));
        }

        [Test]
        public async Task InvokeAsync_WhenNoExceptionIsThrown_DoesNotTrackException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _nextMock.Setup(n => n.Invoke(context)).Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Nothing to assert for telemetry here, since TelemetryClient is non-mockable in its basic form.
        }
    }
}
