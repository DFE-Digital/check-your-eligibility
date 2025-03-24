using AutoFixture;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using Moq;

namespace CheckYourEligibility.API.Tests.UseCases
{
    [TestFixture]
    public class GetApplicationUseCaseTests
    {
        private Mock<IApplication> _mockApplicationGateway;
        private Mock<IAudit> _mockAuditGateway;
        private GetApplicationUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockApplicationGateway = new Mock<IApplication>(MockBehavior.Strict);
            _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new GetApplicationUseCase(_mockApplicationGateway.Object, _mockAuditGateway.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockApplicationGateway.VerifyAll();
            _mockAuditGateway.VerifyAll();
        }

        [Test]
        public async Task Execute_Should_Return_Null_When_Response_Is_Null()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            _mockApplicationGateway.Setup(s => s.GetApplication(guid)).ReturnsAsync((ApplicationResponse)null);

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Call_GetApplication_On_ApplicationGateway()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var response = _fixture.Create<ApplicationResponse>();
            _mockApplicationGateway.Setup(s => s.GetApplication(guid)).ReturnsAsync(response);
            _mockAuditGateway.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Application, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            _mockApplicationGateway.Verify(s => s.GetApplication(guid), Times.Once);
            result.Data.Should().Be(response);
        }
    }
}