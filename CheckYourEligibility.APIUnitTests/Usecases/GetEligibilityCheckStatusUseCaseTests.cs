using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Exceptions;
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
            _mockCheckService.Setup(s => s.GetStatus(guid)).ReturnsAsync((CheckEligibilityStatus?)null);

            // Act
            Func<Task> act = async () => await _sut.Execute(guid);

            // Assert
            act.Should().ThrowAsync<NotFoundException>().WithMessage($"Bulk upload with ID {guid} not found");
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
            result.Data.Should().NotBeNull();
            result.Data.Status.Should().Be(expectedStausCode.ToString());
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
    }
}