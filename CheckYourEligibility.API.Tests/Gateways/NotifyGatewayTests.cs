using System.Security.Claims;
using AutoFixture;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Gateways;
using CheckYourEligibility.API.Gateways.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Notify.Interfaces;

namespace CheckYourEligibility.API.Tests;

public class NotifyGatewayTests : TestBase.TestBase
{
    private Mock<INotificationClientFactory> _mockClientFactory;
    private Mock<INotificationClient> _mockClient;
    private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private NotifyGateway _sut;

    [SetUp]
    public void Setup()
    {
        _mockClient = new Mock<INotificationClient>();
        _mockClientFactory = new Mock<INotificationClientFactory>();
        _mockClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_mockClient.Object);
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
    }

    [TearDown]
    public void Teardown()
    {
        // Clean up resources if needed
    }

    private NotifyGateway BuildSut(Dictionary<string, string> configData, ClaimsPrincipal? user = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var httpContext = new DefaultHttpContext();
        if (user != null) httpContext.User = user;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        return new NotifyGateway(_mockClientFactory.Object, configuration, _mockHttpContextAccessor.Object);
    }

    private static ClaimsPrincipal ClientPrincipal(string clientId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, clientId) },
            "AuthenticationTypes.Federation");
        return new ClaimsPrincipal(identity);
    }

    [Test]
    public void SendNotification_WithNoNotifyKeyConfigured_DoesNotCreateClient()
    {
        // Arrange
        _sut = BuildSut(new Dictionary<string, string>
        {
            { "Notify:Templates:ParentApplicationSuccessful", "mock_id" }
        });
        var notificationRequest = _fixture.Create<NotificationRequest>();
        notificationRequest.Data.Type = NotificationType.ParentApplicationSuccessful;

        // Act
        _sut.SendNotification(notificationRequest);

        // Assert
        _mockClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void SendNotification_WithNotifyKeyConfigured_UsesLiveKey()
    {
        // Arrange
        _sut = BuildSut(new Dictionary<string, string>
        {
            { "Notify:Templates:ParentApplicationSuccessful", "mock_id" },
            { "Notify:Key", "live-key" }
        }, ClientPrincipal("free-school-meals-frontend"));
        var notificationRequest = _fixture.Create<NotificationRequest>();
        notificationRequest.Data.Type = NotificationType.ParentApplicationSuccessful;

        // Act
        _sut.SendNotification(notificationRequest);

        // Assert
        _mockClientFactory.Verify(f => f.CreateClient("live-key"), Times.Once);
        _mockClient.Verify(
            c => c.SendEmail(notificationRequest.Data.Email, "mock_id", notificationRequest.Data.Personalisation,
                null, null, null), Times.Once);
    }

    [Test]
    public void SendNotification_ForClientInTestModeClients_UsesTestKeyInstead()
    {
        // Arrange
        _sut = BuildSut(new Dictionary<string, string>
        {
            { "Notify:Templates:ParentApplicationSuccessful", "mock_id" },
            { "Notify:Key", "live-key" },
            { "Notify:TestKey", "test-key" },
            { "Notify:TestModeClients", "Cypress" }
        }, ClientPrincipal("Cypress"));
        var notificationRequest = _fixture.Create<NotificationRequest>();
        notificationRequest.Data.Type = NotificationType.ParentApplicationSuccessful;

        // Act
        _sut.SendNotification(notificationRequest);

        // Assert
        _mockClientFactory.Verify(f => f.CreateClient("test-key"), Times.Once);
        _mockClientFactory.Verify(f => f.CreateClient("live-key"), Times.Never);
    }

    [Test]
    public void SendNotification_ForSharedClientWithTestModeUserName_UsesTestKeyInstead()
    {
        // Arrange
        _sut = BuildSut(new Dictionary<string, string>
        {
            { "Notify:Templates:ParentApplicationSuccessful", "mock_id" },
            { "Notify:Key", "live-key" },
            { "Notify:TestKey", "test-key" },
            { "Notify:TestModeClients", "Cypress,ece.service+cypress@service.education.gov.uk" }
        }, ClientPrincipal("Omni:ece.service+cypress@service.education.gov.uk"));
        var notificationRequest = _fixture.Create<NotificationRequest>();
        notificationRequest.Data.Type = NotificationType.ParentApplicationSuccessful;

        // Act
        _sut.SendNotification(notificationRequest);

        // Assert
        _mockClientFactory.Verify(f => f.CreateClient("test-key"), Times.Once);
        _mockClientFactory.Verify(f => f.CreateClient("live-key"), Times.Never);
    }

    [Test]
    public void SendNotification_ForSharedClientWithDifferentUserName_UsesLiveKey()
    {
        // Arrange
        _sut = BuildSut(new Dictionary<string, string>
        {
            { "Notify:Templates:ParentApplicationSuccessful", "mock_id" },
            { "Notify:Key", "live-key" },
            { "Notify:TestKey", "test-key" },
            { "Notify:TestModeClients", "ece.service+cypress@service.education.gov.uk" }
        }, ClientPrincipal("Omni:someone-else@service.education.gov.uk"));
        var notificationRequest = _fixture.Create<NotificationRequest>();
        notificationRequest.Data.Type = NotificationType.ParentApplicationSuccessful;

        // Act
        _sut.SendNotification(notificationRequest);

        // Assert
        _mockClientFactory.Verify(f => f.CreateClient("live-key"), Times.Once);
        _mockClientFactory.Verify(f => f.CreateClient("test-key"), Times.Never);
    }

    [Test]
    public void SendNotification_ForClientInTestModeClients_WithNoTestKeyConfigured_FallsBackToLiveKey()
    {
        // Arrange
        _sut = BuildSut(new Dictionary<string, string>
        {
            { "Notify:Templates:ParentApplicationSuccessful", "mock_id" },
            { "Notify:Key", "live-key" },
            { "Notify:TestModeClients", "Cypress" }
        }, ClientPrincipal("Cypress"));
        var notificationRequest = _fixture.Create<NotificationRequest>();
        notificationRequest.Data.Type = NotificationType.ParentApplicationSuccessful;

        // Act
        _sut.SendNotification(notificationRequest);

        // Assert
        _mockClientFactory.Verify(f => f.CreateClient("live-key"), Times.Once);
    }

    [Test]
    public void SendNotification_ClientNotInTestModeClients_UsesLiveKeyEvenWithTestKeyConfigured()
    {
        // Arrange
        _sut = BuildSut(new Dictionary<string, string>
        {
            { "Notify:Templates:ParentApplicationSuccessful", "mock_id" },
            { "Notify:Key", "live-key" },
            { "Notify:TestKey", "test-key" },
            { "Notify:TestModeClients", "Cypress" }
        }, ClientPrincipal("free-school-meals-frontend"));
        var notificationRequest = _fixture.Create<NotificationRequest>();
        notificationRequest.Data.Type = NotificationType.ParentApplicationSuccessful;

        // Act
        _sut.SendNotification(notificationRequest);

        // Assert
        _mockClientFactory.Verify(f => f.CreateClient("live-key"), Times.Once);
        _mockClientFactory.Verify(f => f.CreateClient("test-key"), Times.Never);
    }
}