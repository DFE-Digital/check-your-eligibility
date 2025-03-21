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
using NotFoundException = Ardalis.GuardClauses.NotFoundException;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class GetBulkUploadResultsUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockCheckService;
        private Mock<IAudit> _mockAuditService;
        private Mock<ILogger<GetBulkUploadResultsUseCase>> _mockLogger;
        private GetBulkUploadResultsUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockCheckService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<GetBulkUploadResultsUseCase>>(MockBehavior.Loose);
            _sut = new GetBulkUploadResultsUseCase(_mockCheckService.Object, _mockAuditService.Object, _mockLogger.Object);
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
            Action act = () => new GetBulkUploadResultsUseCase(checkService, auditService, logger);

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
            Action act = () => new GetBulkUploadResultsUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_logger_is_null()
        {
            // Arrange
            var checkService = _mockCheckService.Object;
            var auditService = _mockAuditService.Object;
            ILogger<GetBulkUploadResultsUseCase> logger = null;

            // Act
            Action act = () => new GetBulkUploadResultsUseCase(checkService, auditService, logger);

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
            act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid Request, group ID is required.");
        }

        [Test]
        public async Task Execute_returns_notFound_when_service_returns_null()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            _mockCheckService.Setup(s => s.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid)).ReturnsAsync((IList<CheckEligibilityItem>)null);

            // Act
            Func<Task> act = async () => await _sut.Execute(guid);

            // Assert
            act.Should().ThrowAsync<NotFoundException>().WithMessage($"Bulk upload with ID {guid} not found");
        }

        [Test]
        public async Task Execute_returns_success_with_correct_data_when_service_returns_results()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var resultItems = _fixture.CreateMany<CheckEligibilityItem>().ToList();
            _mockCheckService.Setup(s => s.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid)).ReturnsAsync(resultItems);
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.CheckBulkResults, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.Data.Should().BeEquivalentTo(resultItems);
        }

        [Test]
        public async Task Execute_calls_service_GetBulkCheckResults_with_correct_guid()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var resultItems = _fixture.CreateMany<CheckEligibilityItem>().ToList();
            _mockCheckService.Setup(s => s.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid)).ReturnsAsync(resultItems);
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.CheckBulkResults, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(guid);

            // Assert
            _mockCheckService.Verify(s => s.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid), Times.Once);
        }

        [Test]
        public async Task Execute_calls_auditService_AuditDataGet_with_correct_parameters()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var resultItems = _fixture.CreateMany<CheckEligibilityItem>().ToList();
            _mockCheckService.Setup(s => s.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid)).ReturnsAsync(resultItems);
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.CheckBulkResults, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(guid);

            // Assert
            _mockAuditService.Verify(a => a.CreateAuditEntry(AuditType.CheckBulkResults, guid), Times.Once);
        }
    }
}