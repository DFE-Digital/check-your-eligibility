using AutoFixture;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using Moq;

namespace CheckYourEligibility.API.Tests.UseCases
{
    [TestFixture]
    public class CleanUpEligibilityChecksUseCaseTests: TestBase.TestBase
    {
        private Mock<IAdministration> _mockGateway;
        private Mock<IAudit> _mockAuditGateway;
        private CleanUpEligibilityChecksUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockGateway = new Mock<IAdministration>(MockBehavior.Strict);
            _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new CleanUpEligibilityChecksUseCase(_mockGateway.Object, _mockAuditGateway.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockGateway.VerifyAll();
            _mockAuditGateway.VerifyAll();
        }

        [Test]
        public async Task Execute_Should_Call_CleanUpEligibilityChecks_On_gateway()
        {
            // Arrange
            _mockGateway.Setup(s => s.CleanUpEligibilityChecks()).Returns(Task.CompletedTask);
            _mockAuditGateway.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Administration, string.Empty)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute();

            // Assert
            _mockGateway.Verify(s => s.CleanUpEligibilityChecks(), Times.Once);
        }
    }
}