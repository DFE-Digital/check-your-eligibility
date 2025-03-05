using AutoFixture;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class SearchApplicationsUseCaseTests
    {
        private Mock<IApplication> _mockApplicationService;
        private Mock<IAudit> _mockAuditService;
        private SearchApplicationsUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockApplicationService = new Mock<IApplication>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new SearchApplicationsUseCase(_mockApplicationService.Object, _mockAuditService.Object);
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
            var model = _fixture.Create<ApplicationRequestSearch>();
            _mockApplicationService.Setup(s => s.GetApplications(model)).ReturnsAsync((ApplicationSearchResponse)null);

            // Act
            var result = await _sut.Execute(model);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Return_Null_When_Response_Data_Is_Empty()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            var response = _fixture.Build<ApplicationSearchResponse>()
                                .With(r => r.Data, Enumerable.Empty<ApplicationResponse>().ToList())
                                .Create();
            _mockApplicationService.Setup(s => s.GetApplications(model)).ReturnsAsync(response);

            // Act
            var result = await _sut.Execute(model);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Call_GetApplications_On_ApplicationService()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            var response = _fixture.Create<ApplicationSearchResponse>();
            _mockApplicationService.Setup(s => s.GetApplications(model)).ReturnsAsync(response);
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Application, string.Empty)).Returns((AuditData)null);

            // Act
            var result = await _sut.Execute(model);

            // Assert
            _mockApplicationService.Verify(s => s.GetApplications(model), Times.Once);
            result.Should().Be(response);
        }

        [Test]
        public async Task Execute_Should_Call_AuditAdd_When_AuditData_Is_Not_Null()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            var response = _fixture.Create<ApplicationSearchResponse>();
            var auditData = _fixture.Create<AuditData>();
            _mockApplicationService.Setup(s => s.GetApplications(model)).ReturnsAsync(response);
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Application, string.Empty)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(model);

            // Assert
            _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
        }

        [Test]
        public async Task Execute_Should_Not_Call_AuditAdd_When_AuditData_Is_Null()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            var response = _fixture.Create<ApplicationSearchResponse>();
            _mockApplicationService.Setup(s => s.GetApplications(model)).ReturnsAsync(response);
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Application, string.Empty)).Returns((AuditData)null);

            // Act
            await _sut.Execute(model);

            // Assert
            _mockAuditService.Verify(a => a.AuditAdd(It.IsAny<AuditData>()), Times.Never);
        }
    }
}