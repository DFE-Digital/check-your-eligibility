using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using System.Security.Claims;
using System.Collections.Generic;
using CheckYourEligibility.WebApp.Middleware;

namespace CheckYourEligibility.WebAppUnitTests.Middleware
{
    [TestFixture]
    public class UserTelemetryInitializerTests
    {
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private UserTelemetryInitializer _telemetryInitializer;

        [SetUp]
        public void SetUp()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _telemetryInitializer = new UserTelemetryInitializer(_httpContextAccessorMock.Object);
        }

        [Test]
        public void Initialize_WhenHttpContextIsNull_DoesNotThrowException()
        {
            // Arrange
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null);
            var telemetry = new RequestTelemetry();

            // Act & Assert
            Assert.DoesNotThrow(() => _telemetryInitializer.Initialize(telemetry));
        }

        [Test]
        public void Initialize_WhenUserIsNotAuthenticated_DoesNotSetTelemetryProperties()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
            var telemetry = new RequestTelemetry();

            // Act
            _telemetryInitializer.Initialize(telemetry);

            // Assert
            Assert.That(telemetry.Context.User.AuthenticatedUserId, Is.Null);
            Assert.That(telemetry.Context.User.AccountId, Is.Null);
            Assert.That(telemetry.Context.User.Id, Is.Null);
        }

        [Test]
        public void Initialize_WhenUserIsAuthenticated_SetsTelemetryProperties()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Email, "test-user@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
            var telemetry = new RequestTelemetry();

            // Act
            _telemetryInitializer.Initialize(telemetry);

            // Assert
            Assert.That(telemetry.Context.User.AuthenticatedUserId, Is.EqualTo("test-user-id"));
            Assert.That(telemetry.Context.User.AccountId, Is.EqualTo("test-user@example.com"));
            Assert.That(telemetry.Context.User.Id, Is.EqualTo(identity.Name));
        }

        [Test]
        public void Initialize_WhenUserIsAuthenticated_SetsTelemetryCustomProperties()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Email, "test-user@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
            var telemetry = new TraceTelemetry();

            // Act
            _telemetryInitializer.Initialize(telemetry);

            // Assert
            Assert.That(telemetry.Properties.ContainsKey("UserId"), Is.True);
            Assert.That(telemetry.Properties["UserId"], Is.EqualTo("test-user-id"));
            Assert.That(telemetry.Properties.ContainsKey("UserEmail"), Is.True);
            Assert.That(telemetry.Properties["UserEmail"], Is.EqualTo("test-user@example.com"));
        }
    }
}