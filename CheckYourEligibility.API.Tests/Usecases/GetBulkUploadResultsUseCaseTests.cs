using AutoFixture;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Domain.Exceptions;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.API.Tests.UseCases;

[TestFixture]
public class GetBulkUploadResultsUseCaseTests : TestBase.TestBase
{
    [SetUp]
    public void Setup()
    {
        _mockCheckGateway = new Mock<ICheckEligibility>(MockBehavior.Strict);
        _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<GetBulkUploadResultsUseCase>>(MockBehavior.Loose);
        _sut = new GetBulkUploadResultsUseCase(_mockCheckGateway.Object, _mockAuditGateway.Object, _mockLogger.Object);
        _fixture = new Fixture();
    }

    [TearDown]
    public void Teardown()
    {
        _mockCheckGateway.VerifyAll();
        _mockAuditGateway.VerifyAll();
    }

    private Mock<ICheckEligibility> _mockCheckGateway;
    private Mock<IAudit> _mockAuditGateway;
    private Mock<ILogger<GetBulkUploadResultsUseCase>> _mockLogger;
    private GetBulkUploadResultsUseCase _sut;
    private Fixture _fixture;

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
    public async Task Execute_returns_notFound_when_gateway_returns_null()
    {
        // Arrange
        var guid = _fixture.Create<string>();
        _mockCheckGateway.Setup(s => s.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid))
            .ReturnsAsync((IList<CheckEligibilityItem>)null);

        // Act
        Func<Task> act = async () => await _sut.Execute(guid);

        // Assert
        act.Should().ThrowAsync<NotFoundException>().WithMessage($"Bulk upload with ID {guid} not found");
    }

    [Test]
    public async Task Execute_returns_success_with_correct_data_when_gateway_returns_results()
    {
        // Arrange
        var guid = _fixture.Create<string>();
        var resultItems = _fixture.CreateMany<CheckEligibilityItem>().ToList();
        _mockCheckGateway.Setup(s => s.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid))
            .ReturnsAsync(resultItems);
        _mockAuditGateway.Setup(a => a.CreateAuditEntry(AuditType.CheckBulkResults, guid))
            .ReturnsAsync(_fixture.Create<string>());

        // Act
        var result = await _sut.Execute(guid);

        // Assert
        result.Data.Should().BeEquivalentTo(resultItems);
    }

    [Test]
    public async Task Execute_calls_gateway_GetBulkCheckResults_with_correct_guid()
    {
        // Arrange
        var guid = _fixture.Create<string>();
        var resultItems = _fixture.CreateMany<CheckEligibilityItem>().ToList();
        _mockCheckGateway.Setup(s => s.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid))
            .ReturnsAsync(resultItems);
        _mockAuditGateway.Setup(a => a.CreateAuditEntry(AuditType.CheckBulkResults, guid))
            .ReturnsAsync(_fixture.Create<string>());

        // Act
        await _sut.Execute(guid);

        // Assert
        _mockCheckGateway.Verify(s => s.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid), Times.Once);
    }

    [Test]
    public async Task Execute_calls_auditService_AuditDataGet_with_correct_parameters()
    {
        // Arrange
        var guid = _fixture.Create<string>();
        var resultItems = _fixture.CreateMany<CheckEligibilityItem>().ToList();
        _mockCheckGateway.Setup(s => s.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid))
            .ReturnsAsync(resultItems);
        _mockAuditGateway.Setup(a => a.CreateAuditEntry(AuditType.CheckBulkResults, guid))
            .ReturnsAsync(_fixture.Create<string>());

        // Act
        await _sut.Execute(guid);

        // Assert
        _mockAuditGateway.Verify(a => a.CreateAuditEntry(AuditType.CheckBulkResults, guid), Times.Once);
    }
}