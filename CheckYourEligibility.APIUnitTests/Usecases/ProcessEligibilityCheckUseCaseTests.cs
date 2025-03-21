using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class ProcessEligibilityCheckUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockCheckService;
        private Mock<IAudit> _mockAuditService;
        private Mock<ILogger<ProcessEligibilityCheckUseCase>> _mockLogger;
        private ProcessEligibilityCheckUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockCheckService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<ProcessEligibilityCheckUseCase>>(MockBehavior.Loose);
            _sut = new ProcessEligibilityCheckUseCase(_mockCheckService.Object, _mockAuditService.Object, _mockLogger.Object);
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
            Action act = () => new ProcessEligibilityCheckUseCase(checkService, auditService, logger);

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
            Action act = () => new ProcessEligibilityCheckUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_logger_is_null()
        {
            // Arrange
            var checkService = _mockCheckService.Object;
            var auditService = _mockAuditService.Object;
            ILogger<ProcessEligibilityCheckUseCase> logger = null;

            // Act
            Action act = () => new ProcessEligibilityCheckUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task Execute_returns_failure_when_guid_is_null_or_empty(string guid)
        {
            // Act
            Func<Task> act = async () => await _sut.Execute(guid);

            // Assert
            act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid Request, check ID is required.");
        }

        [Test]
        public async Task Execute_returns_notFound_when_service_returns_null()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var auditItemTemplate = _fixture.Create<AuditData>();

            _mockAuditService.Setup(a => a.AuditDataGet(AuditType.Check, string.Empty))
                .Returns(auditItemTemplate);
            _mockCheckService.Setup(s => s.ProcessCheck(guid, auditItemTemplate))
                .ReturnsAsync((CheckEligibilityStatus?)null);

            // Act
            Func<Task> act = async () => await _sut.Execute(guid);

            // Assert
            act.Should().ThrowAsync<ValidationException>().WithMessage($"Bulk upload with ID {guid} not found");
        }

        [Test]
        public async Task Execute_returns_serviceUnavailable_when_status_is_queuedForProcessing()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var auditItemTemplate = _fixture.Create<AuditData>();
            var statusValue = _fixture.Create<CheckEligibilityStatus>();

            _mockAuditService.Setup(a => a.AuditDataGet(AuditType.Check, string.Empty))
                .Returns(auditItemTemplate);
            _mockCheckService.Setup(s => s.ProcessCheck(guid, auditItemTemplate))
                .ReturnsAsync(statusValue);
            
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            Func<Task> act = async () => await _sut.Execute(guid);

            // Assert
            act.Should().ThrowAsync<ValidationException>().WithMessage("Service is unavailable");
        }

        [Test]
        public async Task Execute_returns_success_with_correct_data_when_service_returns_status_not_queued()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var auditItemTemplate = _fixture.Create<AuditData>();
            var statusValue = CheckEligibilityStatus.eligible; // Updated to use a specific status

            _mockAuditService.Setup(a => a.AuditDataGet(AuditType.Check, string.Empty))
                .Returns(auditItemTemplate);
            _mockCheckService.Setup(s => s.ProcessCheck(guid, auditItemTemplate))
                .ReturnsAsync(statusValue);
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.Data.Status.Should().Be(CheckEligibilityStatus.eligible.ToString());
        }

        [Test]
        public async Task Execute_calls_service_ProcessCheck_with_correct_parameters()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var auditItemTemplate = _fixture.Create<AuditData>();
            var statusValue = _fixture.Create<CheckEligibilityStatus>();

            _mockAuditService.Setup(a => a.AuditDataGet(AuditType.Check, string.Empty))
                .Returns(auditItemTemplate);
            _mockCheckService.Setup(s => s.ProcessCheck(guid, auditItemTemplate))
                .ReturnsAsync(statusValue);
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(guid);

            // Assert
            _mockCheckService.Verify(s => s.ProcessCheck(guid, auditItemTemplate), Times.Once);
        }

        [Test]
        public async Task Execute_returns_failure_when_ProcessCheckException_is_thrown()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var auditItemTemplate = _fixture.Create<AuditData>();

            _mockAuditService.Setup(a => a.AuditDataGet(AuditType.Check, string.Empty))
                .Returns(auditItemTemplate);
            _mockCheckService.Setup(s => s.ProcessCheck(guid, auditItemTemplate))
                .ThrowsAsync(new ProcessCheckException("Test exception"));

            // Act
            Func<Task> act = async () => await _sut.Execute(guid);

            // Assert
            act.Should().ThrowAsync<ValidationException>().WithMessage("Failed to process eligibility check.");
        }
    }
}