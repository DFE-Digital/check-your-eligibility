using AutoFixture;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Moq;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class CleanUpEligibilityChecksUseCaseTests: TestBase.TestBase
    {
        private Mock<IAdministration> _mockService;
        private Mock<IAudit> _mockAuditService;
        private CleanUpEligibilityChecksUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IAdministration>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new CleanUpEligibilityChecksUseCase(_mockService.Object, _mockAuditService.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockService.VerifyAll();
            _mockAuditService.VerifyAll();
        }

        [Test]
        public async Task Execute_Should_Call_CleanUpEligibilityChecks_On_Service()
        {
            // Arrange
            _mockService.Setup(s => s.CleanUpEligibilityChecks()).Returns(Task.CompletedTask);
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Administration, string.Empty)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute();

            // Assert
            _mockService.Verify(s => s.CleanUpEligibilityChecks(), Times.Once);
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            IAdministration service = null;
            IAudit auditService = _mockAuditService.Object;

            // Act
            Action act = () => new CleanUpEligibilityChecksUseCase(service, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_auditService_is_null()
        {
            // Arrange
            IAdministration service = _mockService.Object;
            IAudit auditService = null;

            // Act
            Action act = () => new CleanUpEligibilityChecksUseCase(service, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }
    }
}