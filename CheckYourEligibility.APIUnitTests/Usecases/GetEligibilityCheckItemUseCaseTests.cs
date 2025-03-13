using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
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
    public class GetEligibilityCheckItemUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockCheckService;
        private Mock<IAudit> _mockAuditService;
        private Mock<ILogger<GetEligibilityCheckItemUseCase>> _mockLogger;
        private GetEligibilityCheckItemUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockCheckService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<GetEligibilityCheckItemUseCase>>(MockBehavior.Loose);
            _sut = new GetEligibilityCheckItemUseCase(_mockCheckService.Object, _mockAuditService.Object, _mockLogger.Object);
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
            Action act = () => new GetEligibilityCheckItemUseCase(checkService, auditService, logger);

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
            Action act = () => new GetEligibilityCheckItemUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_logger_is_null()
        {
            // Arrange
            var checkService = _mockCheckService.Object;
            var auditService = _mockAuditService.Object;
            ILogger<GetEligibilityCheckItemUseCase> logger = null;

            // Act
            Action act = () => new GetEligibilityCheckItemUseCase(checkService, auditService, logger);

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
            _mockCheckService.Setup(s => s.GetItem<CheckEligibilityItem>(guid)).ReturnsAsync((CheckEligibilityItem)null);

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.IsValid.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.ValidationErrors.Should().Be($"Bulk upload with ID {guid} not found");
            result.Response.Should().BeNull();
        }

        [Test]
        public async Task Execute_returns_success_with_correct_data_and_links_when_service_returns_item()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var item = _fixture.Create<CheckEligibilityItem>();
            _mockCheckService.Setup(s => s.GetItem<CheckEligibilityItem>(guid)).ReturnsAsync(item);
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.IsValid.Should().BeTrue();
            result.IsNotFound.Should().BeFalse();
            result.Response.Should().NotBeNull();
            result.Response.Data.Should().Be(item);
            result.Response.Links.Should().NotBeNull();
            result.Response.Links.Get_EligibilityCheck.Should().Be($"{CheckLinks.GetLink}{guid}");
            result.Response.Links.Put_EligibilityCheckProcess.Should().Be($"{CheckLinks.ProcessLink}{guid}");
            result.Response.Links.Get_EligibilityCheckStatus.Should().Be($"{CheckLinks.GetLink}{guid}/Status");
        }

        [Test]
        public async Task Execute_calls_service_GetItem_with_correct_guid()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var item = _fixture.Create<CheckEligibilityItem>();
            _mockCheckService.Setup(s => s.GetItem<CheckEligibilityItem>(guid)).ReturnsAsync(item);
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(guid);

            // Assert
            _mockCheckService.Verify(s => s.GetItem<CheckEligibilityItem>(guid), Times.Once);
            _mockAuditService.Verify(a => a.CreateAuditEntry(AuditType.Check, guid), Times.Once);
        }
    }
}