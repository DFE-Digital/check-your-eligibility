using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
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
    public class UpdateEligibilityCheckStatusUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockCheckService;
        private Mock<IAudit> _mockAuditService;
        private Mock<ILogger<UpdateEligibilityCheckStatusUseCase>> _mockLogger;
        private UpdateEligibilityCheckStatusUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockCheckService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<UpdateEligibilityCheckStatusUseCase>>(MockBehavior.Loose);
            _sut = new UpdateEligibilityCheckStatusUseCase(_mockCheckService.Object, _mockAuditService.Object, _mockLogger.Object);
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
            Action act = () => new UpdateEligibilityCheckStatusUseCase(checkService, auditService, logger);

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
            Action act = () => new UpdateEligibilityCheckStatusUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_logger_is_null()
        {
            // Arrange
            var checkService = _mockCheckService.Object;
            var auditService = _mockAuditService.Object;
            ILogger<UpdateEligibilityCheckStatusUseCase> logger = null;

            // Act
            Action act = () => new UpdateEligibilityCheckStatusUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task Execute_returns_failure_when_guid_is_null_or_empty(string guid)
        {
            // Arrange
            var request = _fixture.Create<EligibilityStatusUpdateRequest>();

            // Act
            var result = await _sut.Execute(guid, request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ValidationErrors.Should().Be("Invalid Request, check ID is required.");
            result.Response.Should().BeNull();
        }

        [Test]
        public async Task Execute_returns_failure_when_model_is_null()
        {
            // Arrange
            var guid = _fixture.Create<string>();

            // Act
            var result = await _sut.Execute(guid, null);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ValidationErrors.Should().Be("Invalid Request, update data is required.");
            result.Response.Should().BeNull();
        }

        [Test]
        public async Task Execute_returns_failure_when_model_data_is_null()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var request = new EligibilityStatusUpdateRequest { Data = null };

            // Act
            var result = await _sut.Execute(guid, request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ValidationErrors.Should().Be("Invalid Request, update data is required.");
            result.Response.Should().BeNull();
        }

        [Test]
        public async Task Execute_returns_notFound_when_service_returns_null()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var request = _fixture.Create<EligibilityStatusUpdateRequest>();
            
            _mockCheckService
                .Setup(s => s.UpdateEligibilityCheckStatus(guid, request.Data))
                .ReturnsAsync((CheckEligibilityStatusResponse)null);

            // Act
            var result = await _sut.Execute(guid, request);

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
            var request = _fixture.Create<EligibilityStatusUpdateRequest>();
            var responseData = _fixture.Create<CheckEligibilityStatusResponse>();
            
            _mockCheckService
                .Setup(s => s.UpdateEligibilityCheckStatus(guid, request.Data))
                .ReturnsAsync(responseData);
            
            
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(guid, request);

            // Assert
            result.IsValid.Should().BeTrue();
            result.IsNotFound.Should().BeFalse();
            result.Response.Should().NotBeNull();
            result.Response.Data.Should().Be(responseData.Data);
        }

        [Test]
        public async Task Execute_calls_service_UpdateEligibilityCheckStatus_with_correct_parameters()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var request = _fixture.Create<EligibilityStatusUpdateRequest>();
            var responseData = _fixture.Create<CheckEligibilityStatusResponse>();
            
            _mockCheckService
                .Setup(s => s.UpdateEligibilityCheckStatus(guid, request.Data))
                .ReturnsAsync(responseData);
            
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, guid)).ReturnsAsync(_fixture.Create<string>());
            

            // Act
            await _sut.Execute(guid, request);

            // Assert
            _mockCheckService.Verify(s => s.UpdateEligibilityCheckStatus(guid, request.Data), Times.Once);
        }
    }
}