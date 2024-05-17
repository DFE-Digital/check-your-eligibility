using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;

namespace CheckYourEligibility.APIUnitTests
{
    public class AdministrationControllerTests : TestBase.TestBase
    {
        private Mock<IAdministration> _mockService;
        private ILogger<AdministrationController> _mockLogger;
        private AdministrationController _sut;
        private Mock<IAudit> _mockAuditService;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IAdministration>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<AdministrationController>>();
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new AdministrationController(_mockLogger, _mockService.Object, _mockAuditService.Object);
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
            IAdministration service = null;
            IAudit auditService = null;

            // Act
            Action act = () => new AdministrationController(_mockLogger, service,auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public void Given_CleanUpEligibilityChecks_Should_Return_Status200OK()
        {
            // Arrange
            _mockService.Setup(cs => cs.CleanUpEligibilityChecks()).Returns(Task.CompletedTask);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{Admin.EligibilityChecksCleanse}" }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.CleanUpEligibilityChecks();

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }
    }
}