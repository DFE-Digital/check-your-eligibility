using AutoFixture;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Moq;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class GetApplicationUseCaseTests
    {
        private Mock<IApplication> _mockApplicationService;
        private Mock<IAudit> _mockAuditService;
        private GetApplicationUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockApplicationService = new Mock<IApplication>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new GetApplicationUseCase(_mockApplicationService.Object, _mockAuditService.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockApplicationService.VerifyAll();
            _mockAuditService.VerifyAll();
        }

        [Test]
        public async Task Execute_Should_Return_Null_When_Response_Is_Null()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            _mockApplicationService.Setup(s => s.GetApplication(guid)).ReturnsAsync((ApplicationResponse)null);

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Call_GetApplication_On_ApplicationService()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var response = _fixture.Create<ApplicationResponse>();
            _mockApplicationService.Setup(s => s.GetApplication(guid)).ReturnsAsync(response);
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Application, guid)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(guid);

            // Assert
            _mockApplicationService.Verify(s => s.GetApplication(guid), Times.Once);
            result.Data.Should().Be(response);
        }
    }
}