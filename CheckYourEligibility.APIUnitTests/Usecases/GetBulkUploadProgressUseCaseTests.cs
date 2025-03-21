using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Exceptions;
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
    public class GetBulkUploadProgressUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockCheckService;
        private Mock<ILogger<GetBulkUploadProgressUseCase>> _mockLogger;
        private GetBulkUploadProgressUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockCheckService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<GetBulkUploadProgressUseCase>>(MockBehavior.Loose);
            _sut = new GetBulkUploadProgressUseCase(_mockCheckService.Object, _mockLogger.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockCheckService.VerifyAll();
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
            var guid = _fixture.Create<Guid>().ToString();
            _mockCheckService.Setup(s => s.GetBulkStatus(guid)).ReturnsAsync((BulkStatus)null);

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
            var statusValue = _fixture.Create<BulkStatus>();
            _mockCheckService.Setup(s => s.GetBulkStatus(guid)).ReturnsAsync(statusValue);

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.Data.Should().Be(statusValue);
            result.Links.Should().NotBeNull();
            result.Links.Get_BulkCheck_Results.Should().Be($"{CheckLinks.BulkCheckLink}{guid}{CheckLinks.BulkCheckResults}");
        }

        [Test]
        public async Task Execute_calls_service_GetBulkStatus_with_correct_guid()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var statusValue = _fixture.Create<BulkStatus>();
            _mockCheckService.Setup(s => s.GetBulkStatus(guid)).ReturnsAsync(statusValue);

            // Act
            await _sut.Execute(guid);

            // Assert
            _mockCheckService.Verify(s => s.GetBulkStatus(guid), Times.Once);
        }
    }
}