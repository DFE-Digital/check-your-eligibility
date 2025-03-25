using AutoFixture;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using Moq;

namespace CheckYourEligibility.API.Tests.UseCases;

[TestFixture]
public class SearchApplicationsUseCaseTests
{
    [SetUp]
    public void Setup()
    {
        _mockApplicationGateway = new Mock<IApplication>(MockBehavior.Strict);
        _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
        _sut = new SearchApplicationsUseCase(_mockApplicationGateway.Object, _mockAuditGateway.Object);
        _fixture = new Fixture();
    }

    [TearDown]
    public void Teardown()
    {
        _mockApplicationGateway.VerifyAll();
        _mockAuditGateway.VerifyAll();
    }

    private Mock<IApplication> _mockApplicationGateway;
    private Mock<IAudit> _mockAuditGateway;
    private SearchApplicationsUseCase _sut;
    private Fixture _fixture;

    [Test]
    public async Task Execute_Should_Return_Null_When_Response_Is_Null()
    {
        // Arrange
        var model = _fixture.Create<ApplicationRequestSearch>();
        _mockApplicationGateway.Setup(s => s.GetApplications(model)).ReturnsAsync((ApplicationSearchResponse)null);

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
        _mockApplicationGateway.Setup(s => s.GetApplications(model)).ReturnsAsync(response);

        // Act
        var result = await _sut.Execute(model);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task Execute_Should_Call_GetApplications_On_ApplicationGateway()
    {
        // Arrange
        var model = _fixture.Create<ApplicationRequestSearch>();
        var response = _fixture.Create<ApplicationSearchResponse>();
        _mockApplicationGateway.Setup(s => s.GetApplications(model)).ReturnsAsync(response);
        _mockAuditGateway.Setup(a => a.CreateAuditEntry(AuditType.Administration, string.Empty))
            .ReturnsAsync(_fixture.Create<string>());

        // Act
        var result = await _sut.Execute(model);

        // Assert
        _mockApplicationGateway.Verify(s => s.GetApplications(model), Times.Once);
        result.Should().Be(response);
    }
}