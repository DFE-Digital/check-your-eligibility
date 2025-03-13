using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class GetEligibilityCheckStatusUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockCheckService;
        private Mock<IAudit> _mockAuditService;
        private Mock<ILogger<GetEligibilityCheckStatusUseCase>> _mockLogger;
        private GetEligibilityCheckStatusUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockCheckService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<GetEligibilityCheckStatusUseCase>>(MockBehavior.Loose);
            _sut = new GetEligibilityCheckStatusUseCase(_mockCheckService.Object, _mockAuditService.Object, _mockLogger.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockCheckService.VerifyAll();
            _mockAuditService.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_checkService_is_null()
        {
            // Arrange
            ICheckEligibility checkService = null;
            var auditService = _mockAuditService.Object;
            var logger = _mockLogger.Object;

            // Act
            Action act = () => new GetEligibilityCheckStatusUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'checkService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_auditService_is_null()
        {
            // Arrange
            var checkService = _mockCheckService.Object;
            IAudit auditService = null;
            var logger = _mockLogger.Object;

            // Act
            Action act = () => new GetEligibilityCheckStatusUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_logger_is_null()
        {
            // Arrange
            var checkService = _mockCheckService.Object;
            var auditService = _mockAuditService.Object;
            ILogger<GetEligibilityCheckStatusUseCase> logger = null;

            // Act
            Action act = () => new GetEligibilityCheckStatusUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task Execute_returns_failure_when_guid_is_null_or_empty(string guid)
        {
            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ValidationErrors.Should().Be("Invalid Request, check ID is required.");
            result.Response.Should().BeNull();
        }

        [Test]
        public async Task Execute_returns_notFound_when_service_returns_null()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            _mockCheckService.Setup(s => s.GetStatus(guid)).ReturnsAsync((CheckEligibilityStatus?)null);

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.IsValid.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.ValidationErrors.Should().Be($"Bulk upload with ID {guid} not found");
            result.Response.Should().BeNull();
        }

        [Test]
        public async Task Execute_returns_success_with_correct_data_when_service_returns_status()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var statusValue = _fixture.Create<CheckEligibilityStatus>();
            _mockCheckService.Setup(s => s.GetStatus(guid)).ReturnsAsync(statusValue);

            var expectedStausCode = CheckEligibilityStatus.queuedForProcessing;
            
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.IsValid.Should().BeTrue();
            result.IsNotFound.Should().BeFalse();
            result.Response.Should().NotBeNull();
            result.Response.Data.Should().NotBeNull();
            result.Response.Data.Status.Should().Be(expectedStausCode.ToString());
        }

        [Test]
        public async Task Execute_calls_service_GetStatus_with_correct_guid()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var statusValue = _fixture.Create<CheckEligibilityStatus>();
            _mockCheckService.Setup(s => s.GetStatus(guid)).ReturnsAsync(statusValue);
            
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(guid);

            // Assert
            _mockCheckService.Verify(s => s.GetStatus(guid), Times.Once);
        }

        /* [Test]
        public async Task Execute_calls_audit_service_with_correct_data()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var statusValue = _fixture.Create<CheckEligibilityStatus>();
            _mockCheckService.Setup(s => s.GetStatus(guid)).ReturnsAsync(statusValue);
            
            var auditData = _fixture.Create<AuditData>();
            _mockAuditService.Setup(a => a.AuditDataGet(AuditType.Check, guid)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(guid);

            // Assert
            _mockAuditService.Verify(a => a.AuditDataGet(AuditType.Check, guid), Times.Once);
            _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
        }

        [Test]
        public async Task Execute_does_not_call_auditAdd_when_auditDataGet_returns_null()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var statusValue = _fixture.Create<CheckEligibilityStatus>();
            _mockCheckService.Setup(s => s.GetStatus(guid)).ReturnsAsync(statusValue);
            
            _mockAuditService.Setup(a => a.AuditDataGet(AuditType.Check, guid)).Returns((AuditData)null);

            // Act
            await _sut.Execute(guid);

            // Assert
            _mockAuditService.Verify(a => a.AuditDataGet(AuditType.Check, guid), Times.Once);
            _mockAuditService.Verify(a => a.AuditAdd(It.IsAny<AuditData>()), Times.Never);
        } */
    }
}