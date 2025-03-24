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
    public class UpdateApplicationStatusUseCaseTests
    {
        private Mock<IApplication> _mockApplicationGateway;
        private Mock<IAudit> _mockAuditGateway;
        private UpdateApplicationStatusUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockApplicationGateway = new Mock<IApplication>(MockBehavior.Strict);
            _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new UpdateApplicationStatusUseCase(_mockApplicationGateway.Object, _mockAuditGateway.Object);
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
            var model = _fixture.Create<ApplicationStatusUpdateRequest>();
            _mockApplicationGateway.Setup(s => s.UpdateApplicationStatus(guid, model.Data)).ReturnsAsync((ApplicationStatusUpdateResponse)null);

            // Act
            var result = await _sut.Execute(guid, model);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Call_UpdateApplicationStatus_On_ApplicationGateway()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var model = _fixture.Create<ApplicationStatusUpdateRequest>();
            var response = _fixture.Create<ApplicationStatusUpdateResponse>();
            _mockApplicationGateway.Setup(s => s.UpdateApplicationStatus(guid, model.Data)).ReturnsAsync(response);
            _mockAuditGateway.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Application, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(guid, model);

            // Assert
            _mockApplicationGateway.Verify(s => s.UpdateApplicationStatus(guid, model.Data), Times.Once);
            result.Data.Should().Be(response.Data);
        }
    }
}