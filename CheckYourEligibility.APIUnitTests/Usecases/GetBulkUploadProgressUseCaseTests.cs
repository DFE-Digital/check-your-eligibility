using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

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
        public void Constructor_throws_argumentNullException_when_checkService_is_null()
        {
            // Arrange
            ICheckEligibility checkService = null;
            var logger = _mockLogger.Object;

            // Act
            Action act = () => new GetBulkUploadProgressUseCase(checkService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'checkService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_logger_is_null()
        {
            // Arrange
            var checkService = _mockCheckService.Object;
            ILogger<GetBulkUploadProgressUseCase> logger = null;

            // Act
            Action act = () => new GetBulkUploadProgressUseCase(checkService, logger);

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
            result.ValidationErrors.Should().Be("Invalid Request, group ID is required.");
            result.Response.Should().BeNull();
        }

        [Test]
        public async Task Execute_returns_notFound_when_service_returns_null()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockCheckService.Setup(s => s.GetBulkStatus(guid)).ReturnsAsync((BulkStatus)null);

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
            var statusValue = _fixture.Create<BulkStatus>();
            _mockCheckService.Setup(s => s.GetBulkStatus(guid)).ReturnsAsync(statusValue);

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.IsValid.Should().BeTrue();
            result.IsNotFound.Should().BeFalse();
            result.Response.Should().NotBeNull();
            result.Response.Data.Should().Be(statusValue);
            result.Response.Links.Should().NotBeNull();
            result.Response.Links.Get_BulkCheck_Results.Should().Be($"{CheckLinks.BulkCheckLink}{guid}{CheckLinks.BulkCheckResults}");
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